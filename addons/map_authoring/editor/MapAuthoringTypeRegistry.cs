namespace ProceduralRts.MapAuthoring.Editor;

public sealed record MapAuthoringTypeDescriptor(
    string Name,
    string ScriptPath);

public static class MapAuthoringTypeRegistry
{
    public const string BaseType = "Node2D";

    public static IReadOnlyList<MapAuthoringTypeDescriptor> Types { get; } = Array.AsReadOnly(new[]
    {
        Type("MapRoot", "MapRoot"),
        Type("OwnerStart", "OwnerStart"),
        Type("Building", "Building"),
        Type("Unit", "Unit"),
        Type("ResourceField", "Resource"),
        Type("Obstacle", "Obstacle"),
        Type("TerrainRegion", "TerrainRegion"),
        Type("Trigger", "Trigger"),
        Type("Objective", "Objective"),
        Type("Narrative", "Narrative"),
    });

    internal static void ValidateTypeNames(
        IReadOnlyList<MapAuthoringTypeDescriptor> types,
        Func<string, bool> nativeClassExists)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var descriptor in types)
        {
            if (!names.Add(descriptor.Name))
            {
                throw new InvalidOperationException($"Duplicate custom type name '{descriptor.Name}'.");
            }
            if (nativeClassExists(descriptor.Name))
            {
                throw new InvalidOperationException(
                    $"Custom type name '{descriptor.Name}' collides with native Godot class '{descriptor.Name}'.");
            }
        }
    }

    private static MapAuthoringTypeDescriptor Type(string name, string scriptName)
    {
        return new MapAuthoringTypeDescriptor(
            name,
            $"res://addons/map_authoring/nodes/{scriptName}.cs");
    }
}
