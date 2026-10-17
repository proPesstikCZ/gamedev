using System;
using System.IO;
using Game.Core;
using Game.Data;
using UnityEngine;

namespace Game.Runtime
{
    /// <summary>
    /// Drives the simulation in a scene: turns real time into game weeks according to the selected
    /// speed (GDD 3: pause and 3 speeds), starts new games and saves or loads the single save slot.
    /// </summary>
    public sealed class GameRunner : MonoBehaviour
    {
        public enum Speed
        {
            Paused = 0,
            Normal = 1,
            Fast = 2,
            Fastest = 3,
        }

        private const int MaxWeeksPerFrame = 10;

        [SerializeField] private GameDatabase database;
        [Tooltip("Seed of the first game and of new games when Use Fixed Seed is on.")]
        [SerializeField] private long seed = 1972;
        [SerializeField] private bool useFixedSeed;
        [SerializeField] private Speed speed = Speed.Normal;
        [Header("Real seconds per game week")]
        [SerializeField, Min(0.01f)] private float normalSecondsPerWeek = 1f;
        [SerializeField, Min(0.01f)] private float fastSecondsPerWeek = 0.4f;
        [SerializeField, Min(0.01f)] private float fastestSecondsPerWeek = 0.1f;
        [Header("Saving")]
        [SerializeField] private string saveFileName = "save.json";
        [Tooltip("Saves to the slot at the start of every month (GDD 17, PLAN 3.6).")]
        [SerializeField] private bool autosave = true;
        [Header("Debug")]
        [SerializeField] private bool logWeeks;

        private float _elapsed;
        private GameDatabaseData _data;
        // False while the slot holds a save the player has not chosen to replace (it failed to load
        // at startup). Autosave must not overwrite it until the player starts, loads or saves a game.
        private bool _autosaveAllowed = true;

        /// <summary>
        /// Save file used by runners created from now on, before their Awake reads the slot.
        /// Tests set it so they never read or overwrite the player's save. Null = inspector value.
        /// </summary>
        public static string SaveFileNameOverride { get; set; }

        public GameSimulation Simulation { get; private set; }
        public Speed CurrentSpeed => speed;
        public GameDatabaseData Data => _data;
        public string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);
        public bool HasSave => File.Exists(SavePath);
        /// <summary>Result of loading the slot at startup; null when there was no save.</summary>
        public SaveLoadError? StartupLoadResult { get; private set; }
        /// <summary>False when autosave is held back to protect a save that could not be loaded.</summary>
        public bool AutosaveAllowed => _autosaveAllowed;

        /// <summary>File name of the save slot in persistentDataPath. Tests use their own file.</summary>
        public string SaveFileName
        {
            get => saveFileName;
            set => saveFileName = string.IsNullOrWhiteSpace(value) ? "save.json" : value;
        }

        /// <summary>A new or loaded game replaced the previous simulation.</summary>
        public event Action<GameSimulation> SimulationStarted;
        /// <summary>Raised after every simulated week.</summary>
        public event Action WeekAdvanced;
        public event Action<ReleasedGame> GameReleased;
        public event Action<ReleasedGame> SalesEnded;
        public event Action<System.Collections.Generic.IReadOnlyList<TrendNews>> TrendNewsAnnounced;
        public event Action Bankrupted;
        public event Action<Speed> SpeedChanged;
        /// <summary>Raised after an automatic save; the argument tells whether it succeeded.</summary>
        public event Action<bool> Autosaved;

        private void Awake()
        {
            if (database == null)
            {
                Debug.LogError("[GameRunner] No GameDatabase assigned.", this);
                enabled = false;
                return;
            }

            _data = database.ToCore();
            var errors = DatabaseValidator.Validate(_data);
            if (errors.Count > 0)
            {
                foreach (var error in errors) Debug.LogError($"[GameRunner] Content error: {error}", database);
                enabled = false;
                return;
            }

            if (!string.IsNullOrWhiteSpace(SaveFileNameOverride)) saveFileName = SaveFileNameOverride;
            // Continue the saved game, so the first autosave never replaces it with a fresh one.
            if (HasSave)
            {
                var result = Load();
                StartupLoadResult = result;
                if (result == SaveLoadError.None) return;
            }
            StartSimulation(GameSimulation.NewGame(_data, unchecked((ulong)seed)));
            // A save that exists but did not load stays untouched until the player decides.
            _autosaveAllowed = StartupLoadResult == null;
        }

        private void Update()
        {
            if (Simulation == null || Simulation.IsGameOver || speed == Speed.Paused) return;

            float interval = SecondsPerWeek(speed);
            _elapsed += Time.deltaTime;
            int steps = 0;
            while (_elapsed >= interval && steps < MaxWeeksPerFrame && !Simulation.IsGameOver && speed != Speed.Paused)
            {
                _elapsed -= interval;
                steps++;
                Step();
            }
            if (steps == MaxWeeksPerFrame) _elapsed = 0f;
        }

        /// <summary>Simulates one week right away, whatever the speed. Used by the runner and by tests.</summary>
        public void Step()
        {
            if (Simulation == null || Simulation.IsGameOver) return;
            int month = Simulation.Date.Month;
            Simulation.AdvanceWeek();
            if (logWeeks) Debug.Log($"[GameRunner] {Simulation.Date} money {Simulation.Studio.Money}", this);
            WeekAdvanced?.Invoke();
            // Saved after the whole week is done, so the save never holds a half-simulated week.
            if (autosave && _autosaveAllowed && !Simulation.IsGameOver && Simulation.Date.Month != month)
                Autosaved?.Invoke(Save());
        }

        public void SetSpeed(Speed newSpeed)
        {
            if (speed == newSpeed) return;
            speed = newSpeed;
            _elapsed = 0f;
            SpeedChanged?.Invoke(speed);
        }

        /// <summary>Starts a new game. Without a seed it uses a random one unless Use Fixed Seed is on.</summary>
        public void NewGame(ulong? newSeed = null)
        {
            if (_data == null) return;
            ulong s = newSeed ?? (useFixedSeed ? unchecked((ulong)seed) : (ulong)DateTime.UtcNow.Ticks);
            StartSimulation(GameSimulation.NewGame(_data, s));
            _autosaveAllowed = true;
        }

        /// <summary>Writes the current game to the save slot. Returns false when writing failed.</summary>
        public bool Save()
        {
            if (Simulation == null) return false;
            try
            {
                string json = SaveSerializer.ToJson(Simulation.ToSave());
                string path = SavePath;
                string temp = path + ".tmp";
                File.WriteAllText(temp, json);
                // Replace in one step, so a crash while writing never leaves a half-written save.
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
                // The player chose to overwrite the slot, so autosave may keep it up to date.
                _autosaveAllowed = true;
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[GameRunner] Save failed: {e.Message}", this);
                return false;
            }
        }

        /// <summary>
        /// Loads the save slot. On any error the running game stays as it was: the save is fully
        /// checked (format, version and references to content) before it replaces the game.
        /// </summary>
        public SaveLoadError Load()
        {
            if (_data == null) return SaveLoadError.Corrupt;
            string json;
            try
            {
                json = File.ReadAllText(SavePath);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[GameRunner] Load failed: {e.Message}", this);
                return SaveLoadError.Corrupt;
            }

            var save = SaveSerializer.TryFromJson(json, out var error);
            if (save == null) return error;
            GameSimulation loaded;
            try
            {
                loaded = GameSimulation.FromSave(_data, save);
            }
            catch (Exception e) when (e is ArgumentException || e is FormatException || e is InvalidOperationException)
            {
                Debug.LogWarning($"[GameRunner] Save does not fit the content: {e.Message}", this);
                return SaveLoadError.Corrupt;
            }
            StartSimulation(loaded);
            _autosaveAllowed = true;
            return SaveLoadError.None;
        }

        private void StartSimulation(GameSimulation simulation)
        {
            if (Simulation != null)
            {
                Simulation.GameReleased -= OnGameReleased;
                Simulation.SalesEnded -= OnSalesEnded;
                Simulation.TrendNewsAnnounced -= OnTrendNews;
                Simulation.Studio.Bankrupted -= OnBankrupted;
            }
            Simulation = simulation;
            _elapsed = 0f;
            Simulation.GameReleased += OnGameReleased;
            Simulation.SalesEnded += OnSalesEnded;
            Simulation.TrendNewsAnnounced += OnTrendNews;
            Simulation.Studio.Bankrupted += OnBankrupted;
            Debug.Log($"[GameRunner] Game started {Simulation.Date}, money {Simulation.Studio.Money}.", this);
            SimulationStarted?.Invoke(Simulation);
        }

        private void OnGameReleased(ReleasedGame game) => GameReleased?.Invoke(game);
        private void OnSalesEnded(ReleasedGame game) => SalesEnded?.Invoke(game);
        private void OnTrendNews(System.Collections.Generic.IReadOnlyList<TrendNews> news) => TrendNewsAnnounced?.Invoke(news);

        private void OnBankrupted()
        {
            Debug.Log($"[GameRunner] Bankrupt on {Simulation.Date}.", this);
            Bankrupted?.Invoke();
        }

        private float SecondsPerWeek(Speed s)
        {
            switch (s)
            {
                case Speed.Fast: return fastSecondsPerWeek;
                case Speed.Fastest: return fastestSecondsPerWeek;
                default: return normalSecondsPerWeek;
            }
        }
    }
}
