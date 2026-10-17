using System;

namespace Game.Core
{
    /// <summary>One value (0..1) per development area. Used for genre weights.</summary>
    [Serializable]
    public struct AreaWeights
    {
        public float GameplayDesign;
        public float StoryAndWorld;
        public float Technology;
        public float Gameplay;
        public float Graphics;
        public float Sound;
        public float LevelDesign;
        public float AiAndSystems;
        public float DialogueAndText;
        public float Polish;

        public float this[DevArea area]
        {
            get
            {
                switch (area)
                {
                    case DevArea.GameplayDesign: return GameplayDesign;
                    case DevArea.StoryAndWorld: return StoryAndWorld;
                    case DevArea.Technology: return Technology;
                    case DevArea.Gameplay: return Gameplay;
                    case DevArea.Graphics: return Graphics;
                    case DevArea.Sound: return Sound;
                    case DevArea.LevelDesign: return LevelDesign;
                    case DevArea.AiAndSystems: return AiAndSystems;
                    case DevArea.DialogueAndText: return DialogueAndText;
                    case DevArea.Polish: return Polish;
                    default: throw new ArgumentOutOfRangeException(nameof(area), area, null);
                }
            }
            set
            {
                switch (area)
                {
                    case DevArea.GameplayDesign: GameplayDesign = value; break;
                    case DevArea.StoryAndWorld: StoryAndWorld = value; break;
                    case DevArea.Technology: Technology = value; break;
                    case DevArea.Gameplay: Gameplay = value; break;
                    case DevArea.Graphics: Graphics = value; break;
                    case DevArea.Sound: Sound = value; break;
                    case DevArea.LevelDesign: LevelDesign = value; break;
                    case DevArea.AiAndSystems: AiAndSystems = value; break;
                    case DevArea.DialogueAndText: DialogueAndText = value; break;
                    case DevArea.Polish: Polish = value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(area), area, null);
                }
            }
        }

        public float Sum()
        {
            float sum = 0f;
            for (int i = 0; i < DevAreas.Count; i++) sum += this[(DevArea)i];
            return sum;
        }
    }
}
