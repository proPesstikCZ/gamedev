using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{
    /// <summary>All static game content, independent of Unity assets.</summary>
    public sealed class GameDatabaseData
    {
        private readonly Dictionary<string, GenreData> _genres = new Dictionary<string, GenreData>();
        private readonly Dictionary<string, ThemeData> _themes = new Dictionary<string, ThemeData>();
        private readonly Dictionary<string, PlatformData> _platforms = new Dictionary<string, PlatformData>();

        public IReadOnlyList<GenreData> Genres { get; }
        public IReadOnlyList<ThemeData> Themes { get; }
        public IReadOnlyList<PlatformData> Platforms { get; }
        public BalanceData Balance { get; }

        public GameDatabaseData(IReadOnlyList<GenreData> genres, IReadOnlyList<ThemeData> themes,
            IReadOnlyList<PlatformData> platforms, BalanceData balance)
        {
            Genres = genres ?? new GenreData[0];
            Themes = themes ?? new ThemeData[0];
            Platforms = platforms ?? new PlatformData[0];
            Balance = balance;

            // Duplicates are reported by DatabaseValidator; the first entry wins here.
            foreach (var g in Genres) if (g?.Id != null && !_genres.ContainsKey(g.Id)) _genres.Add(g.Id, g);
            foreach (var t in Themes) if (t?.Id != null && !_themes.ContainsKey(t.Id)) _themes.Add(t.Id, t);
            foreach (var p in Platforms) if (p?.Id != null && !_platforms.ContainsKey(p.Id)) _platforms.Add(p.Id, p);
        }

        public GenreData GetGenre(string id) => id != null && _genres.TryGetValue(id, out var v) ? v : null;
        public ThemeData GetTheme(string id) => id != null && _themes.TryGetValue(id, out var v) ? v : null;
        public PlatformData GetPlatform(string id) => id != null && _platforms.TryGetValue(id, out var v) ? v : null;

        public IEnumerable<GenreData> AvailableGenres(int year) => Genres.Where(g => g != null && g.IsAvailable(year));
        public IEnumerable<ThemeData> AvailableThemes(int year) => Themes.Where(t => t != null && t.IsAvailable(year));
        public IEnumerable<PlatformData> AvailablePlatforms(int year) => Platforms.Where(p => p != null && p.IsAvailable(year));
    }
}
