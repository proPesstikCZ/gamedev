namespace Game.Core
{
    /// <summary>Development areas across all phases (GDD 6.2).</summary>
    public enum DevArea
    {
        // Pre-production
        GameplayDesign = 0,
        StoryAndWorld = 1,
        Technology = 2,
        // Production
        Gameplay = 3,
        Graphics = 4,
        Sound = 5,
        LevelDesign = 6,
        // Finishing
        AiAndSystems = 7,
        DialogueAndText = 8,
        Polish = 9,
    }

    public static class DevAreas
    {
        public const int Count = 10;
    }
}
