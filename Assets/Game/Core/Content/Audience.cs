using System;

namespace Game.Core
{
    public enum Audience
    {
        Kids = 0,
        Everyone = 1,
        Adults = 2,
    }

    /// <summary>How well a genre fits each audience (0..1, 1 = perfect fit).</summary>
    [Serializable]
    public struct AudienceFit
    {
        public float Kids;
        public float Everyone;
        public float Adults;

        public AudienceFit(float kids, float everyone, float adults)
        {
            Kids = kids;
            Everyone = everyone;
            Adults = adults;
        }

        public float this[Audience audience]
        {
            get
            {
                switch (audience)
                {
                    case Audience.Kids: return Kids;
                    case Audience.Everyone: return Everyone;
                    case Audience.Adults: return Adults;
                    default: throw new ArgumentOutOfRangeException(nameof(audience), audience, null);
                }
            }
        }
    }
}
