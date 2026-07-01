namespace ProceduralRts.Core;

/// <summary>
/// Deterministic, self-owned pseudo-random source for the simulation. Gameplay
/// randomness (damage variance, ballistic spread, AI jitter) must come from here
/// so that the same seed plus the same command log reproduces the same state
/// hash. Presentation may use Godot's RNG freely; the simulation must not.
///
/// Implementation: SplitMix64 — small, fast, fully reproducible, no platform
/// dependence on floating point seeding.
/// </summary>
public sealed class DeterministicRng
{
    private ulong _state;

    public DeterministicRng(ulong seed)
    {
        _state = seed;
    }

    /// <summary>Current internal state, folded into the world state hash.</summary>
    public ulong State => _state;

    public ulong NextU64()
    {
        unchecked
        {
            _state += 0x9E3779B97F4A7C15UL;
            var z = _state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }

    /// <summary>Float in [0, 1).</summary>
    public float NextFloat()
    {
        // Top 24 bits give a uniform float with full mantissa precision.
        return (NextU64() >> 40) * (1f / (1 << 24));
    }

    /// <summary>Float in [min, max).</summary>
    public float NextRange(float min, float max)
    {
        return min + (NextFloat() * (max - min));
    }

    /// <summary>Integer in [minInclusive, maxExclusive).</summary>
    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            return minInclusive;
        }

        var span = (ulong)(maxExclusive - minInclusive);
        return minInclusive + (int)(NextU64() % span);
    }
}
