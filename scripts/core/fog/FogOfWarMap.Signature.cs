using Godot;

namespace ProceduralRts.Core;

public sealed partial class FogOfWarMap
{
    private static ulong VisionSourceSignature(
        Vector2 worldSize,
        IReadOnlyList<(Vector2 Position, float SightRange)> sources)
    {
        var hash = 14695981039346656037UL;
        hash = MixHash(hash, BitConverter.SingleToInt32Bits(worldSize.X));
        hash = MixHash(hash, BitConverter.SingleToInt32Bits(worldSize.Y));
        hash = MixHash(hash, sources.Count);
        for (var index = 0; index < sources.Count; index++)
        {
            var source = sources[index];
            hash = MixHash(hash, BitConverter.SingleToInt32Bits(source.Position.X));
            hash = MixHash(hash, BitConverter.SingleToInt32Bits(source.Position.Y));
            hash = MixHash(hash, BitConverter.SingleToInt32Bits(source.SightRange));
        }

        return hash;
    }

    private static ulong MixHash(ulong hash, int value)
    {
        unchecked
        {
            hash ^= (uint)value;
            hash *= 1099511628211UL;
            hash ^= (uint)(value >> 16);
            hash *= 1099511628211UL;
            return hash;
        }
    }
}
