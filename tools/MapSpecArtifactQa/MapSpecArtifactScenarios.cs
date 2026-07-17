using System.Text;
using ProceduralRts.Core;

static class MapSpecArtifactScenarios
{
    public static void Run(List<string> failures)
    {
        var map = ArtifactFixtureMap.Create();
        var first = MapSpecArtifactCodec.Encode(map);
        var second = MapSpecArtifactCodec.Encode(map);
        var bytes = first.ToArray();
        Require(bytes.SequenceEqual(second.ToArray()) && first.Sha256 == second.Sha256, "unchanged encodes must have identical bytes and hash", failures);
        Require(bytes[^1] == (byte)'\n' && bytes[^2] != (byte)'\n', "canonical artifact must have exactly one terminal LF", failures);
        Require(Encoding.UTF8.GetPreamble().Length == 3 && !bytes.AsSpan().StartsWith(Encoding.UTF8.GetPreamble()), "canonical artifact must not have a UTF-8 BOM", failures);

        var decoded = MapSpecArtifactCodec.Decode(bytes);
        Require(MapSpecArtifactCodec.Encode(decoded).ToArray().SequenceEqual(bytes), "schema round-trip must preserve exact canonical bytes", failures);
        Require(decoded.TerrainCells.Select(item => item.Id).SequenceEqual(["SoftRoad", "CatBasePad"]), "terrain source order must survive round-trip", failures);
        Require(AllCollectionsPreserved(decoded), "round-trip must preserve every MapSpec collection", failures);

        var negativeZero = MapSpecArtifactCodec.Encode(ArtifactFixtureMap.Create(-0f)).ToArray();
        var positiveZero = MapSpecArtifactCodec.Encode(ArtifactFixtureMap.Create(0f)).ToArray();
        Require(negativeZero.SequenceEqual(positiveZero) && !Encoding.UTF8.GetString(negativeZero).Contains("-0", StringComparison.Ordinal), "negative zero must normalize to positive zero", failures);
        ValidateFactionWire(failures);

        var callerOwned = ArtifactFixtureMap.Create();
        var buildings = callerOwned.Buildings.ToList();
        callerOwned = callerOwned with { Buildings = buildings };
        var snapshot = MapSpecArtifactCodec.Encode(callerOwned);
        buildings.Clear();
        var exposedCopy = snapshot.ToArray(); exposedCopy[0] = (byte)'!';
        Require(MapSpecArtifactCodec.Decode(snapshot.ToArray()).Buildings.Count == 2, "artifact must deep-snapshot collections and defensively copy bytes", failures);

        RejectArtifact(() => MapSpecArtifactCodec.Encode(ArtifactFixtureMap.Create(float.NaN)), "non-finite writer input", failures);
        RejectArtifact(() => MapSpecArtifactFactionWire.Write((FactionId)999), "unsupported faction writer value", failures);
        Expect<MapOwnerTopologyValidationException>(() => MapSpecArtifactCodec.Encode(map with { OwnerStarts = [map.OwnerStarts[0]] }), "Encode must use MapLoader owner-topology authority", failures);
        RejectMutations(bytes, failures);
    }

    private static void RejectMutations(byte[] canonical, List<string> failures)
    {
        var text = Encoding.UTF8.GetString(canonical);
        RejectArtifact(() => MapSpecArtifactCodec.Decode([]), "empty input", failures);
        RejectArtifact(() => MapSpecArtifactCodec.Decode(Encoding.UTF8.GetBytes("{")), "malformed JSON", failures);
        RejectArtifact(() => MapSpecArtifactCodec.Decode(Encoding.UTF8.GetBytes(text.Replace("\"schemaVersion\":1", "\"schemaVersion\":2", StringComparison.Ordinal))), "unknown schemaVersion", failures);
        RejectArtifact(() => MapSpecArtifactCodec.Decode(Encoding.UTF8.GetBytes(text.Replace("\"schemaVersion\":1", "\"schemaVersion\":\"1\"", StringComparison.Ordinal))), "wrong schemaVersion type", failures);
        RejectArtifact(() => MapSpecArtifactCodec.Decode(Encoding.UTF8.GetBytes(text.Replace("\"faction\":\"dog\"", "\"faction\":\"Dog\"", StringComparison.Ordinal))), "wrong-case faction wire value", failures);
        RejectArtifact(() => MapSpecArtifactCodec.Decode(Encoding.UTF8.GetBytes(text.Replace("\"faction\":\"dog\"", "\"faction\":\"wolf\"", StringComparison.Ordinal))), "unknown faction wire value", failures);
        RejectArtifact(() => MapSpecArtifactCodec.Decode(Encoding.UTF8.GetBytes(text.Replace("\"map\":", "\"extra\":0,\"map\":", StringComparison.Ordinal))), "unknown envelope field", failures);
        RejectArtifact(() => MapSpecArtifactCodec.Decode(Encoding.UTF8.GetBytes(text.Replace("\"format\":", "\"format\":\"duplicate\",\"format\":", StringComparison.Ordinal))), "duplicate field", failures);
        RejectArtifact(() => MapSpecArtifactCodec.Decode(Encoding.UTF8.GetBytes(text.Replace("\"seed\":20260701,", "", StringComparison.Ordinal))), "missing field", failures);
        RejectArtifact(() => MapSpecArtifactCodec.Decode(Encoding.UTF8.GetBytes(text.Replace("\"seed\":20260701", "\"seed\":2147483648", StringComparison.Ordinal))), "integer overflow", failures);
        RejectArtifact(() => MapSpecArtifactCodec.Decode(Encoding.UTF8.GetBytes(text.Replace("\"facing\":0", "\"facing\":1e400", StringComparison.Ordinal))), "non-finite numeric input", failures);
        RejectArtifact(() => MapSpecArtifactCodec.Decode(Encoding.UTF8.GetBytes(" " + text)), "noncanonical whitespace", failures);
        RejectArtifact(() => MapSpecArtifactCodec.Decode(canonical[..^1]), "missing terminal LF", failures);
        RejectArtifact(() => MapSpecArtifactCodec.Decode(Encoding.UTF8.GetBytes(text + "{}")), "trailing JSON content", failures);
        RejectArtifact(() => MapSpecArtifactCodec.Decode([.. Encoding.UTF8.GetPreamble(), .. canonical]), "UTF-8 BOM", failures);
        RejectArtifact(() => MapSpecArtifactCodec.Decode([0xff, 0xfe, 0xfd]), "malformed UTF-8", failures);
        Expect<MapOwnerTopologyValidationException>(
            () => MapSpecArtifactCodec.Decode(Encoding.UTF8.GetBytes(text.Replace("\"ownerId\":2", "\"ownerId\":3", StringComparison.Ordinal))),
            "Decode must use MapLoader owner-topology authority",
            failures);
    }

    private static void ValidateFactionWire(List<string> failures)
    {
        var expected = new[]
        {
            (FactionId.Dog, "dog"),
            (FactionId.Cat, "cat"),
            (FactionId.Corruption, "corruption"),
        };
        Require(expected.All(pair => MapSpecArtifactFactionWire.Write(pair.Item1) == pair.Item2
                && MapSpecArtifactFactionWire.Read(pair.Item2) == pair.Item1),
            "faction wire mapping must explicitly round-trip every supported value", failures);
    }

    private static bool AllCollectionsPreserved(MapSpec map)
    {
        return map.OwnerStarts.Count == 2 && map.TerrainCells.Count == 2 && map.Resources.Count == 1
            && map.Obstacles.Count == 1 && map.Buildings.Count == 2 && map.Units.Count == 2
            && map.Triggers.Count == 1 && map.Objectives.Count == 1 && map.NarrativeNodes.Count == 1;
    }

    private static void RejectArtifact(Action action, string label, List<string> failures)
    {
        try
        {
            action();
            failures.Add($"codec should reject {label}");
        }
        catch (MapSpecArtifactException)
        {
        }
        catch (Exception exception)
        {
            failures.Add($"codec should reject {label} with MapSpecArtifactException, got {exception.GetType().Name}");
        }
    }

    private static void Expect<TException>(Action action, string label, List<string> failures)
        where TException : Exception
    {
        try
        {
            action();
            failures.Add(label);
        }
        catch (TException)
        {
        }
        catch (Exception exception)
        {
            failures.Add($"{label}; got {exception.GetType().Name}");
        }
    }

    private static void Require(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add(message);
    }
}
