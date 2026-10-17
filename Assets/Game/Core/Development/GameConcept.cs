using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>Game size (GDD 6.5). Only Small is available in the vertical slice.</summary>
    public enum GameSize
    {
        Small = 0,
        Medium = 1,
        Large = 2,
        AAA = 3,
        Mmo = 4,
    }

    public enum EngineChoice
    {
        Custom = 0,
        Licensed = 1,
    }

    /// <summary>What the player chooses before development starts (GDD 6.1).</summary>
    [Serializable]
    public sealed class GameConcept
    {
        [SerializeField] private string name;
        [SerializeField] private string genreId;
        [SerializeField] private string themeId;
        [SerializeField] private string platformId;
        [SerializeField] private GameSize size;
        [SerializeField] private Audience audience;
        [SerializeField] private EngineChoice engine;

        /// <summary>Player-entered title, not a localization key.</summary>
        public string Name => name;
        public string GenreId => genreId;
        public string ThemeId => themeId;
        public string PlatformId => platformId;
        public GameSize Size => size;
        public Audience Audience => audience;
        public EngineChoice Engine => engine;

        private GameConcept() { }

        public GameConcept(string name, string genreId, string themeId, string platformId,
            Audience audience, EngineChoice engine = EngineChoice.Custom, GameSize size = GameSize.Small)
        {
            this.name = name;
            this.genreId = genreId;
            this.themeId = themeId;
            this.platformId = platformId;
            this.size = size;
            this.audience = audience;
            this.engine = engine;
        }
    }

    /// <summary>Reasons a concept cannot be started. UI maps them to localized texts.</summary>
    public enum ConceptError
    {
        EmptyName = 0,
        UnknownGenre = 1,
        GenreNotAvailable = 2,
        UnknownTheme = 3,
        ThemeNotAvailable = 4,
        UnknownPlatform = 5,
        PlatformNotAvailable = 6,
        SizeNotAvailable = 7,
        InvalidAudience = 8,
        InvalidEngine = 9,
        NotEnoughMoney = 10,
        ProjectAlreadyRunning = 11,
        Bankrupt = 12,
    }

    public static class ConceptValidator
    {
        /// <summary>Money paid when development starts: dev kit (once per platform) and a licensed engine.</summary>
        public static long UpfrontCost(GameConcept concept, GameDatabaseData db, StudioState studio)
        {
            if (concept == null || db == null) return 0;
            long cost = 0;
            var platform = db.GetPlatform(concept.PlatformId);
            if (platform != null && (studio == null || !studio.OwnsDevKit(platform.Id))) cost += platform.DevKitCost;
            if (concept.Engine == EngineChoice.Licensed) cost += db.Balance.Development.LicensedEngineCost;
            return cost;
        }

        public static List<ConceptError> Validate(GameConcept concept, GameDatabaseData db, int year, StudioState studio)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            var errors = new List<ConceptError>();
            if (concept == null)
            {
                errors.Add(ConceptError.EmptyName);
                return errors;
            }

            if (string.IsNullOrWhiteSpace(concept.Name)) errors.Add(ConceptError.EmptyName);

            var genre = db.GetGenre(concept.GenreId);
            if (genre == null) errors.Add(ConceptError.UnknownGenre);
            else if (!genre.IsAvailable(year)) errors.Add(ConceptError.GenreNotAvailable);

            var theme = db.GetTheme(concept.ThemeId);
            if (theme == null) errors.Add(ConceptError.UnknownTheme);
            else if (!theme.IsAvailable(year)) errors.Add(ConceptError.ThemeNotAvailable);

            var platform = db.GetPlatform(concept.PlatformId);
            if (platform == null) errors.Add(ConceptError.UnknownPlatform);
            else if (!platform.IsAvailable(year)) errors.Add(ConceptError.PlatformNotAvailable);

            if (concept.Size != GameSize.Small) errors.Add(ConceptError.SizeNotAvailable);
            if (!Enum.IsDefined(typeof(Audience), concept.Audience)) errors.Add(ConceptError.InvalidAudience);
            if (!Enum.IsDefined(typeof(EngineChoice), concept.Engine)) errors.Add(ConceptError.InvalidEngine);

            if (studio != null)
            {
                if (studio.IsBankrupt) errors.Add(ConceptError.Bankrupt);
                if (studio.Money < UpfrontCost(concept, db, studio)) errors.Add(ConceptError.NotEnoughMoney);
            }
            return errors;
        }
    }
}
