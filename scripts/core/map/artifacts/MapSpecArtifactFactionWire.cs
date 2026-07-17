namespace ProceduralRts.Core;

public static class MapSpecArtifactFactionWire
{
    public static string Write(FactionId faction)
    {
        return faction switch
        {
            FactionId.Dog => "dog",
            FactionId.Cat => "cat",
            FactionId.Corruption => "corruption",
            _ => throw new MapSpecArtifactException($"Unsupported faction value '{(int)faction}'."),
        };
    }

    public static FactionId Read(string value)
    {
        return value switch
        {
            "dog" => FactionId.Dog,
            "cat" => FactionId.Cat,
            "corruption" => FactionId.Corruption,
            _ => throw new MapSpecArtifactException($"Unsupported faction wire value '{value}'."),
        };
    }
}
