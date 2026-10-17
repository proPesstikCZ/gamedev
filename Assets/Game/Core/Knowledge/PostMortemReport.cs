using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>One line of the post-mortem: a localization key plus its parameters. No finished sentences here.</summary>
    [Serializable]
    public sealed class PostMortemEntry
    {
        public string Key;
        /// <summary>Ids for the text: genre, theme, area or audience ids, depending on the key.</summary>
        public string[] Args = new string[0];
        public float Value;

        public PostMortemEntry() { }

        public PostMortemEntry(string key, float value, params string[] args)
        {
            Key = key;
            Value = value;
            Args = args ?? new string[0];
        }
    }

    /// <summary>Post-mortem after a release (GDD 6.6), built from ReviewBreakdown.</summary>
    [Serializable]
    public sealed class PostMortemReport
    {
        public List<PostMortemEntry> Entries = new List<PostMortemEntry>();

        public bool Has(string key) => Entries.Exists(e => e.Key == key);
        public PostMortemEntry Find(string key) => Entries.Find(e => e.Key == key);
        public List<PostMortemEntry> FindAll(string key) => Entries.FindAll(e => e.Key == key);
    }

    /// <summary>Text keys used by the post-mortem. Kept in one place so UI and tests share them.</summary>
    public static class PostMortemKeys
    {
        public const string Review = "postmortem.review";
        public const string ExpectationsAbove = "postmortem.expectations.above";
        public const string ExpectationsMet = "postmortem.expectations.met";
        public const string ExpectationsBelow = "postmortem.expectations.below";
        public const string ComboExcellent = "postmortem.combo.excellent";
        public const string ComboGood = "postmortem.combo.good";
        public const string ComboBad = "postmortem.combo.bad";
        public const string AudiencePoor = "postmortem.audience.poor";
        public const string BugsMany = "postmortem.bugs.many";
        public const string BugsFew = "postmortem.bugs.few";
        public const string TrendHot = "postmortem.trend.hot";
        public const string TrendCold = "postmortem.trend.cold";
        /// <summary>"Players of {genre} loved {area}." Value = WeightLevel.</summary>
        public const string HintWeightHigh = "postmortem.hint.weight_high";
        public const string HintWeightMedium = "postmortem.hint.weight_medium";
        /// <summary>"Hardly anyone cared about {area} in {genre}." Value = WeightLevel.</summary>
        public const string HintWeightLow = "postmortem.hint.weight_low";
        public const string StrongestArea = "postmortem.area.strongest";
        public const string WeakestArea = "postmortem.area.weakest";

        /// <summary>Stable id of an area for localization keys, e.g. "level_design".</summary>
        public static string AreaId(DevArea area)
        {
            switch (area)
            {
                case DevArea.GameplayDesign: return "gameplay_design";
                case DevArea.StoryAndWorld: return "story_and_world";
                case DevArea.Technology: return "technology";
                case DevArea.Gameplay: return "gameplay";
                case DevArea.Graphics: return "graphics";
                case DevArea.Sound: return "sound";
                case DevArea.LevelDesign: return "level_design";
                case DevArea.AiAndSystems: return "ai_and_systems";
                case DevArea.DialogueAndText: return "dialogue_and_text";
                case DevArea.Polish: return "polish";
                default: throw new ArgumentOutOfRangeException(nameof(area), area, null);
            }
        }

        public static string AudienceId(Audience audience)
        {
            switch (audience)
            {
                case Audience.Kids: return "kids";
                case Audience.Everyone: return "everyone";
                case Audience.Adults: return "adults";
                default: throw new ArgumentOutOfRangeException(nameof(audience), audience, null);
            }
        }
    }

    public static class PostMortemBuilder
    {
        /// <summary>
        /// Builds the report and reveals knowledge: the combo of this game and a few hidden weights
        /// of its genre. The revealed weights become hints in the report.
        /// </summary>
        public static PostMortemReport Build(GameDatabaseData db, GameConcept concept, ReviewBreakdown review,
            float genreTrend, MarketKnowledge knowledge)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (concept == null) throw new ArgumentNullException(nameof(concept));
            if (review == null) throw new ArgumentNullException(nameof(review));
            if (knowledge == null) throw new ArgumentNullException(nameof(knowledge));
            var balance = db.Balance.PostMortem;
            var genre = db.GetGenre(concept.GenreId) ?? throw new ArgumentException($"Unknown genre '{concept.GenreId}'.", nameof(concept));
            var report = new PostMortemReport();
            var entries = report.Entries;

            entries.Add(new PostMortemEntry(PostMortemKeys.Review, review.Review));

            if (review.QualityRatio >= balance.AboveExpectationRatio) entries.Add(new PostMortemEntry(PostMortemKeys.ExpectationsAbove, review.QualityRatio));
            else if (review.QualityRatio < balance.BelowExpectationRatio) entries.Add(new PostMortemEntry(PostMortemKeys.ExpectationsBelow, review.QualityRatio));
            else entries.Add(new PostMortemEntry(PostMortemKeys.ExpectationsMet, review.QualityRatio));

            string comboKey = review.Combo == ComboRating.Excellent ? PostMortemKeys.ComboExcellent
                : review.Combo == ComboRating.Good ? PostMortemKeys.ComboGood : PostMortemKeys.ComboBad;
            entries.Add(new PostMortemEntry(comboKey, review.ComboFactor, concept.GenreId, concept.ThemeId));
            knowledge.RevealCombo(concept.GenreId, concept.ThemeId);

            if (review.AudienceFactor < balance.PoorAudienceFactor)
                entries.Add(new PostMortemEntry(PostMortemKeys.AudiencePoor, review.AudienceFactor, concept.GenreId, PostMortemKeys.AudienceId(concept.Audience)));

            if (review.BugFactor < balance.ManyBugsFactor) entries.Add(new PostMortemEntry(PostMortemKeys.BugsMany, review.Bugs));
            else if (review.BugFactor > balance.FewBugsFactor) entries.Add(new PostMortemEntry(PostMortemKeys.BugsFew, review.Bugs));

            if (genreTrend >= balance.HotTrend) entries.Add(new PostMortemEntry(PostMortemKeys.TrendHot, genreTrend, concept.GenreId));
            else if (genreTrend < balance.ColdTrend) entries.Add(new PostMortemEntry(PostMortemKeys.TrendCold, genreTrend, concept.GenreId));

            AddStrongestAndWeakest(review, entries);

            foreach (var area in knowledge.PickWeightsToReveal(genre, balance))
            {
                knowledge.RevealWeight(genre.Id, area);
                var level = MarketKnowledge.LevelOf(genre.Weights[area], balance);
                string key = level == WeightLevel.High ? PostMortemKeys.HintWeightHigh
                    : level == WeightLevel.Low ? PostMortemKeys.HintWeightLow : PostMortemKeys.HintWeightMedium;
                entries.Add(new PostMortemEntry(key, (int)level, genre.Id, PostMortemKeys.AreaId(area)));
            }
            return report;
        }

        /// <summary>Areas that added the most and the least to quality, relative to their weight.</summary>
        private static void AddStrongestAndWeakest(ReviewBreakdown review, List<PostMortemEntry> entries)
        {
            int best = -1, worst = -1;
            for (int i = 0; i < DevAreas.Count; i++)
            {
                if (review.Weights[i] <= 0f) continue;
                float c = review.Contribution((DevArea)i);
                if (best < 0 || c > review.Contribution((DevArea)best)) best = i;
                if (worst < 0 || c < review.Contribution((DevArea)worst)) worst = i;
            }
            if (best >= 0) entries.Add(new PostMortemEntry(PostMortemKeys.StrongestArea, review.Contribution((DevArea)best), PostMortemKeys.AreaId((DevArea)best)));
            if (worst >= 0 && worst != best) entries.Add(new PostMortemEntry(PostMortemKeys.WeakestArea, review.Contribution((DevArea)worst), PostMortemKeys.AreaId((DevArea)worst)));
        }
    }
}
