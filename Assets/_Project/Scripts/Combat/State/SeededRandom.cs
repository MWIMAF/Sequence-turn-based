using System;

namespace Game.Combat
{
    /// <summary>
    /// Deterministic RNG (xorshift32). Same seed, same numbers, on every platform and .NET version,
    /// which System.Random does not guarantee. Never use UnityEngine.Random inside Game.Combat.
    /// </summary>
    /// <remarks>
    /// Clash resolution never rolls dice. This RNG is only for things like enemy planning.
    /// </remarks>
    public sealed class SeededRandom
    {
        // Any non-zero value works; xorshift gets stuck at 0.
        private const uint ZeroSeedReplacement = 0x9E3779B9;

        private uint _state;

        public SeededRandom(int seed)
        {
            _state = seed == 0 ? ZeroSeedReplacement : unchecked((uint)seed);
        }

        /// <summary>Current internal state, so a fight can be saved and restored exactly.</summary>
        public uint State => _state;

        public static SeededRandom FromState(uint state)
        {
            var rng = new SeededRandom(1);
            rng._state = state == 0 ? ZeroSeedReplacement : state;
            return rng;
        }

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        /// <summary>Returns an int in [0, maxExclusive).</summary>
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Must be greater than 0.");
            }

            return (int)(NextUInt() % (uint)maxExclusive);
        }
    }
}
