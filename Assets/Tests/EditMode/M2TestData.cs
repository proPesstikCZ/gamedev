using System.Collections.Generic;
using Game.Core;

namespace Game.Tests
{
    /// <summary>Helpers for development, review and sales tests.</summary>
    internal static class M2TestData
    {
        /// <summary>A genre where gameplay matters a lot and story almost not at all.</summary>
        public static AreaWeights UnevenWeights()
        {
            var w = new AreaWeights
            {
                GameplayDesign = 1f, StoryAndWorld = 0.05f, Technology = 0.4f,
                Gameplay = 1f, Graphics = 0.5f, Sound = 0.3f, LevelDesign = 0.1f,
                AiAndSystems = 0.2f, DialogueAndText = 0.05f, Polish = 0.9f,
            };
            return w;
        }

        /// <summary>
        /// Database with an uneven "arcade" genre, an "rpg" from 1979, themes with all combo ratings
        /// and two platforms with different installed bases.
        /// </summary>
        public static GameDatabaseData Database(BalanceData balance = null, AudienceFit? arcadeAudience = null)
        {
            var arcade = new GenreData("arcade", "genre.arcade.name", 1972, UnevenWeights(), arcadeAudience ?? new AudienceFit(1f, 1f, 0.5f));
            var rpg = TestData.Genre("rpg", 1979);
            var genres = new List<GenreData> { arcade, rpg };
            var themes = new List<ThemeData>
            {
                new ThemeData("space", "theme.space.name", 1972, new[] { new GenreCombo("arcade", ComboRating.Excellent), new GenreCombo("rpg", ComboRating.Good) }),
                new ThemeData("pirates", "theme.pirates.name", 1972, new[] { new GenreCombo("arcade", ComboRating.Bad), new GenreCombo("rpg", ComboRating.Good) }),
                new ThemeData("sport", "theme.sport.name", 1972, new[] { new GenreCombo("arcade", ComboRating.Good), new GenreCombo("rpg", ComboRating.Bad) }),
            };
            var small = new PlatformData("small", "platform.small.name", 1972, 1990, 1000, 500, 0.1f,
                new YearCurve(new[] { new YearPoint(1972, 0.2f), new YearPoint(1990, 0.2f) }), null);
            var big = new PlatformData("big", "platform.big.name", 1972, 1990, 1000, 800, 0.1f,
                new YearCurve(new[] { new YearPoint(1972, 2f), new YearPoint(1990, 2f) }), null);
            var computer = new PlatformData("computer", "platform.computer.name", 1977, 1990, 0, 700, 0.2f,
                new YearCurve(new[] { new YearPoint(1977, 0.5f), new YearPoint(1990, 1f) }), null);
            return new GameDatabaseData(genres, themes, new List<PlatformData> { small, big, computer }, balance ?? TestData.Balance(startMoney: 20000));
        }

        public static GameConcept Concept(string genre = "arcade", string theme = "space", string platform = "small",
            Audience audience = Audience.Everyone, EngineChoice engine = EngineChoice.Custom, GameSize size = GameSize.Small)
        {
            return new GameConcept("Test Game", genre, theme, platform, audience, engine, size);
        }

        /// <summary>Gives every area the same number of points.</summary>
        public static float[] FlatPoints(float value)
        {
            var points = new float[DevAreas.Count];
            for (int i = 0; i < points.Length; i++) points[i] = value;
            return points;
        }

        /// <summary>Runs the simulation until the active project is released. Returns the released game.</summary>
        public static ReleasedGame RunUntilReleased(GameSimulation sim, int maxWeeks = 60)
        {
            ReleasedGame released = null;
            void OnReleased(ReleasedGame g) => released = g;
            sim.GameReleased += OnReleased;
            for (int i = 0; i < maxWeeks && released == null && !sim.IsGameOver; i++) sim.AdvanceWeek();
            sim.GameReleased -= OnReleased;
            return released;
        }

        /// <summary>A fixed RNG for formula tests: always returns the same value.</summary>
        public sealed class FixedRandom : IRandom
        {
            private readonly double _value;
            public FixedRandom(double value) { _value = value; }
            public double NextDouble() => _value;
            public int Next(int minInclusive, int maxExclusive) => minInclusive;
            public float Range(float min, float max) => (float)(min + (max - min) * _value);
        }
    }
}
