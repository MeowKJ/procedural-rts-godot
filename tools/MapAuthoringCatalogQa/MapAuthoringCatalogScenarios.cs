using ProceduralRts.Core;

static class MapAuthoringCatalogScenarios
{
    public static void Run(List<string> failures)
    {
        Require(MapAuthoringCatalog.BuildingIds.SequenceEqual(BuildSpecCatalog.Definitions.Keys.Order(StringComparer.Ordinal)),
            "Building options must equal authoritative catalog ids in ordinal order.", failures);
        Require(MapAuthoringCatalog.UnitIds.SequenceEqual(UnitDesignCatalog.Designs.Keys.Order(StringComparer.Ordinal)),
            "Unit options must equal authoritative catalog ids in ordinal order.", failures);
        var factions = FactionCatalog.Definitions.Keys.Select(MapSpecArtifactFactionWire.Write).Order(StringComparer.Ordinal);
        Require(MapAuthoringCatalog.FactionIds.SequenceEqual(factions),
            "Faction options must equal authoritative catalog wire ids in ordinal order.", failures);
        foreach (var options in AllOptions())
        {
            Require(options.SequenceEqual(options.Order(StringComparer.Ordinal)), "Catalog options must keep stable ordinal order.", failures);
            Require(options.Count == options.Distinct(StringComparer.Ordinal).Count(), "Catalog options must be unique.", failures);
            Require(options is IList<string> list && list.IsReadOnly, "Catalog options must expose immutable collections.", failures);
        }

        Require(MapAuthoringKeyCatalog.Require(MapAuthoringKeyKind.Terrain, "ground") == "ground",
            "Supported authoring key should round-trip exactly.", failures);
        Reject(() => MapAuthoringKeyCatalog.Require(MapAuthoringKeyKind.Terrain, "Ground"), "Wrong-case terrain key", failures);
        Reject(() => MapAuthoringCatalog.RequireBuilding("unknown.building"), "Unknown building id", failures);
        Reject(() => MapAuthoringCatalog.RequireUnit("unknown.unit"), "Unknown unit id", failures);
        Reject(() => MapAuthoringCatalog.RequireFaction("corruption"), "Faction absent from FactionCatalog", failures);
    }

    private static IEnumerable<IReadOnlyList<string>> AllOptions()
    {
        yield return MapAuthoringCatalog.BuildingIds;
        yield return MapAuthoringCatalog.UnitIds;
        yield return MapAuthoringCatalog.FactionIds;
        foreach (var kind in Enum.GetValues<MapAuthoringKeyKind>()) yield return MapAuthoringKeyCatalog.Options(kind);
    }

    private static void Reject(Action action, string label, List<string> failures)
    {
        try { action(); failures.Add($"{label} should throw MapAuthoringCatalogException."); }
        catch (MapAuthoringCatalogException) { }
        catch (Exception exception) { failures.Add($"{label} threw {exception.GetType().Name}."); }
    }

    private static void Require(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add(message);
    }
}
