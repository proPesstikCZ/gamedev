using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Data
{
    /// <summary>Entry point to all game content assets.</summary>
    [CreateAssetMenu(menuName = "Game/Game Database", fileName = "GameDatabase")]
    public sealed class GameDatabase : ScriptableObject
    {
        [SerializeField] private List<GenreDefinition> genres = new List<GenreDefinition>();
        [SerializeField] private List<ThemeDefinition> themes = new List<ThemeDefinition>();
        [SerializeField] private List<PlatformDefinition> platforms = new List<PlatformDefinition>();
        [SerializeField] private BalanceConfig balance;

        public GameDatabaseData ToCore()
        {
            var g = new List<GenreData>(genres.Count);
            foreach (var d in genres) g.Add(d != null ? d.ToCore() : null);
            var t = new List<ThemeData>(themes.Count);
            foreach (var d in themes) t.Add(d != null ? d.ToCore() : null);
            var p = new List<PlatformData>(platforms.Count);
            foreach (var d in platforms) p.Add(d != null ? d.ToCore() : null);
            return new GameDatabaseData(g, t, p, balance != null ? balance.ToCore() : null);
        }
    }
}
