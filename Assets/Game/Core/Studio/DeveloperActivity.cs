namespace Game.Core
{
    /// <summary>What the player's character in the garage is doing (PLAN 4.3).</summary>
    public enum DeveloperActivity
    {
        Resting = 0,
        Working = 1,
    }

    public static class DeveloperActivityRule
    {
        /// <summary>Works while a game is in development, otherwise rests. After bankruptcy nobody works.</summary>
        public static DeveloperActivity For(GameSimulation simulation)
        {
            if (simulation == null || simulation.IsGameOver || simulation.ActiveProject == null)
                return DeveloperActivity.Resting;
            return DeveloperActivity.Working;
        }
    }
}
