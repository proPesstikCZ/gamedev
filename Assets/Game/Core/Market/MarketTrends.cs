using System;
using System.Collections.Generic;

namespace Game.Core
{
    public enum TrendSubject
    {
        Genre = 0,
        Theme = 1,
    }

    public enum TrendDirection
    {
        Rising = 0,
        Falling = 1,
    }

    /// <summary>A market news item announcing a trend change ahead (GDD 9). UI builds the text from the keys.</summary>
    public readonly struct TrendNews
    {
        public readonly TrendSubject Subject;
        public readonly string Id;
        /// <summary>Year the change takes effect.</summary>
        public readonly int Year;
        public readonly TrendDirection Direction;
        /// <summary>Relative change, e.g. 0.3 = +30 %.</summary>
        public readonly float Change;

        public TrendNews(TrendSubject subject, string id, int year, TrendDirection direction, float change)
        {
            Subject = subject;
            Id = id;
            Year = year;
            Direction = direction;
            Change = change;
        }

        public string Key => Direction == TrendDirection.Rising ? "news.trend.rising" : "news.trend.falling";
    }

    /// <summary>
    /// Popularity of genres and themes in time: the authored historical curve times a small
    /// generated change per year. The change comes from a hash of the trend seed, so it does not
    /// consume the simulation RNG and is the same after save and load.
    /// </summary>
    public sealed class MarketTrends
    {
        private readonly GameDatabaseData _db;
        private readonly TrendBalance _balance;

        public ulong Seed { get; }

        public MarketTrends(GameDatabaseData db, ulong seed)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _balance = db.Balance?.Trends ?? new TrendBalance();
            Seed = seed;
        }

        public float GenreTrend(string genreId, int year)
        {
            var genre = _db.GetGenre(genreId);
            return genre == null ? 1f : Evaluate(genre.Trend, TrendSubject.Genre, genreId, year);
        }

        public float ThemeTrend(string themeId, int year)
        {
            var theme = _db.GetTheme(themeId);
            return theme == null ? 1f : Evaluate(theme.Trend, TrendSubject.Theme, themeId, year);
        }

        /// <summary>
        /// News announced in <paramref name="year"/>: trends that change noticeably
        /// <see cref="TrendBalance.AnnounceYearsAhead"/> years later. Only content available by then is reported.
        /// </summary>
        public List<TrendNews> NewsForYear(int year)
        {
            var news = new List<TrendNews>();
            int target = year + Math.Max(1, _balance.AnnounceYearsAhead);
            foreach (var g in _db.Genres)
            {
                if (g == null || !g.IsAvailable(target)) continue;
                AddIfChanged(news, TrendSubject.Genre, g.Id, year, target, GenreTrend(g.Id, year), GenreTrend(g.Id, target));
            }
            foreach (var t in _db.Themes)
            {
                if (t == null || !t.IsAvailable(target)) continue;
                AddIfChanged(news, TrendSubject.Theme, t.Id, year, target, ThemeTrend(t.Id, year), ThemeTrend(t.Id, target));
            }
            return news;
        }

        private void AddIfChanged(List<TrendNews> news, TrendSubject subject, string id, int year, int target, float now, float later)
        {
            if (now <= 0f) return;
            float change = later / now - 1f;
            if (change >= _balance.AnnounceThreshold) news.Add(new TrendNews(subject, id, target, TrendDirection.Rising, change));
            else if (change <= -_balance.AnnounceThreshold) news.Add(new TrendNews(subject, id, target, TrendDirection.Falling, change));
        }

        private float Evaluate(YearCurve curve, TrendSubject subject, string id, int year)
        {
            float baseValue = BaseValue(curve, year);
            float noise = (float)(Hash01(Seed, subject, id, year) * 2.0 - 1.0) * _balance.NoiseAmplitude;
            return Math.Max(_balance.MinTrend, baseValue * (1f + noise));
        }

        /// <summary>Empty curve = 1; before the first point the first value holds.</summary>
        private static float BaseValue(YearCurve curve, int year)
        {
            var points = curve.Points;
            if (points.Count == 0) return 1f;
            if (year <= points[0].Year) return points[0].Value;
            return curve.Evaluate(year);
        }

        /// <summary>Stable hash to [0, 1). string.GetHashCode is not stable between runs, so FNV-1a is used.</summary>
        private static double Hash01(ulong seed, TrendSubject subject, string id, int year)
        {
            ulong h = 14695981039346656037UL;
            foreach (char c in id)
            {
                h ^= c;
                h *= 1099511628211UL;
            }
            h ^= (ulong)(int)subject * 0x9E3779B97F4A7C15UL;
            h ^= (ulong)year * 0xBF58476D1CE4E5B9UL;
            var rng = new SeededRandom(seed ^ h);
            return rng.NextDouble();
        }
    }
}
