using Game.Core;
using Game.Data;
using UnityEngine;

namespace Game.Runtime
{
    /// <summary>
    /// Drives the simulation in a scene: turns real time into game weeks
    /// according to the selected speed (GDD 3: pause and 3 speeds).
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
        [SerializeField] private long seed = 1972;
        [SerializeField] private Speed speed = Speed.Normal;
        [Header("Real seconds per game week")]
        [SerializeField, Min(0.01f)] private float normalSecondsPerWeek = 1f;
        [SerializeField, Min(0.01f)] private float fastSecondsPerWeek = 0.4f;
        [SerializeField, Min(0.01f)] private float fastestSecondsPerWeek = 0.1f;
        [Header("Debug")]
        [SerializeField] private bool logWeeks = true;

        private float _elapsed;

        public GameSimulation Simulation { get; private set; }
        public Speed CurrentSpeed => speed;

        private void Awake()
        {
            if (database == null)
            {
                Debug.LogError("[GameRunner] No GameDatabase assigned.", this);
                enabled = false;
                return;
            }

            var data = database.ToCore();
            var errors = DatabaseValidator.Validate(data);
            if (errors.Count > 0)
            {
                foreach (var error in errors) Debug.LogError($"[GameRunner] Content error: {error}", database);
                enabled = false;
                return;
            }

            Simulation = GameSimulation.NewGame(data, unchecked((ulong)seed));
            Simulation.Studio.Bankrupted += () => Debug.Log($"[GameRunner] Bankrupt on {Simulation.Date}.", this);
            Debug.Log($"[GameRunner] New game {Simulation.Date}, money {Simulation.Studio.Money}, seed {seed}.", this);
        }

        private void Update()
        {
            if (Simulation == null || Simulation.IsGameOver || speed == Speed.Paused) return;

            float interval = SecondsPerWeek(speed);
            _elapsed += Time.deltaTime;
            int steps = 0;
            while (_elapsed >= interval && steps < MaxWeeksPerFrame && !Simulation.IsGameOver)
            {
                _elapsed -= interval;
                steps++;
                Simulation.AdvanceWeek();
                if (logWeeks) Debug.Log($"[GameRunner] {Simulation.Date} money {Simulation.Studio.Money}", this);
            }
            if (steps == MaxWeeksPerFrame) _elapsed = 0f;
        }

        public void SetSpeed(Speed newSpeed)
        {
            speed = newSpeed;
            _elapsed = 0f;
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
