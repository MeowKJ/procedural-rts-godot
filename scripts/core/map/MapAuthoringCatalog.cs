namespace ProceduralRts.Core;

public sealed class MapAuthoringCatalogException : InvalidOperationException
{
    public MapAuthoringCatalogException(string catalog, string value)
        : base($"Map authoring {catalog} id '{value}' is not supported.")
    {
        Catalog = catalog;
        Value = value;
    }

    public string Catalog { get; }

    public string Value { get; }
}

public static class MapAuthoringCatalog
{
    public static IReadOnlyList<string> BuildingIds { get; } = Array.AsReadOnly(
        BuildSpecCatalog.Definitions.Keys.Order(StringComparer.Ordinal).ToArray());

    public static IReadOnlyList<string> UnitIds { get; } = Array.AsReadOnly(
        UnitDesignCatalog.Designs.Keys.Order(StringComparer.Ordinal).ToArray());

    public static IReadOnlyList<string> FactionIds { get; } = Array.AsReadOnly(
        FactionCatalog.Definitions.Keys
            .Select(MapSpecArtifactFactionWire.Write)
            .Order(StringComparer.Ordinal)
            .ToArray());

    public static string RequireBuilding(string value)
    {
        return Require(BuildingIds, "building", value);
    }

    public static string RequireUnit(string value)
    {
        return Require(UnitIds, "unit", value);
    }

    public static FactionId RequireFaction(string value)
    {
        if (!FactionIds.Contains(value, StringComparer.Ordinal))
        {
            throw new MapAuthoringCatalogException("faction", value);
        }

        return MapSpecArtifactFactionWire.Read(value);
    }

    private static string Require(IReadOnlyList<string> options, string catalog, string value)
    {
        if (!options.Contains(value, StringComparer.Ordinal))
        {
            throw new MapAuthoringCatalogException(catalog, value);
        }

        return value;
    }
}
