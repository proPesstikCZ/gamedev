using System;

namespace Game.Core
{
    /// <summary>All randomness in the simulation goes through this interface (GDD 17: deterministic per seed).</summary>
    public interface IRandom
    {
        /// <summary>Returns a value in [0, 1).</summary>
        double NextDouble();
        /// <summary>Returns an integer in [minInclusive, maxExclusive).</summary>
        int Next(int minInclusive, int maxExclusive);
        /// <summary>Returns a float in [min, max).</summary>
        float Range(float min, float max);
    }

    /// <summary>
    /// SplitMix64 generator. Its whole state is one 64-bit number, so it can be saved
    /// and restored exactly (unlike System.Random).
    /// </summary>
    public sealed class SeededRandom : IRandom
    {
        private ulong _state;

        public ulong State => _state;

        public SeededRandom(ulong seedOrState)
        {
            _state = seedOrState;
        }

        public ulong NextULong()
        {
            ulong z = _state += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        public double NextDouble()
        {
            return (NextULong() >> 11) * (1.0 / (1UL << 53));
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "Must be greater than minInclusive.");
            ulong range = (ulong)((long)maxExclusive - minInclusive);
            return (int)(minInclusive + (long)(NextULong() % range));
        }

        public float Range(float min, float max)
        {
            return (float)(min + (max - min) * NextDouble());
        }
    }
}
