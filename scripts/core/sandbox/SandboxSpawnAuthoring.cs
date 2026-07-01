using Godot;

namespace ProceduralRts.Core;

public enum SandboxSpawnAuthoringSource
{
    UnitDesign,
    BuildSpec
}

public sealed record SandboxSpawnAuthoringEntry(
    string Id,
    SandboxSpawnAuthoringSource Source,
    EntityKind EntityKind,
    string Category,
    UnitFactionId? Faction,
    string Label,
    string NameKey,
    string ShortCode,
    IconGlyph Icon,
    int TechTier,
    int Cost,
    IReadOnlySet<string> Tags);

public sealed record SandboxSpawnAuthoringQuery(
    EntityKind? EntityKind = null,
    string? Category = null,
    UnitFactionId? Faction = null);

public sealed record SandboxSpawnRequest(
    SandboxSpawnAuthoringEntry Entry,
    EntitySpec Spec,
    OwnerId OwnerId,
    EntityTransform Transform);

public static class SandboxSpawnAuthoring
{
    private static readonly Lazy<IReadOnlyList<SandboxSpawnAuthoringEntry>> CachedEntries = new(BuildEntries);
    private static readonly Lazy<IReadOnlyDictionary<string, EntitySpec>> CachedSpecs = new(BuildSpecs);

    public static IReadOnlyList<SandboxSpawnAuthoringEntry> Entries => CachedEntries.Value;

    public static IReadOnlyList<EntityKind> EntityKinds => Entries
        .Select(entry => entry.EntityKind)
        .Distinct()
        .OrderBy(kind => kind)
        .ToArray();

    public static IReadOnlyList<SandboxSpawnAuthoringEntry> List(SandboxSpawnAuthoringQuery? query = null)
    {
        query ??= new SandboxSpawnAuthoringQuery();

        return Entries
            .Where(entry => query.EntityKind is null || entry.EntityKind == query.EntityKind)
            .Where(entry => query.Category is null || string.Equals(entry.Category, query.Category, StringComparison.OrdinalIgnoreCase))
            .Where(entry => query.Faction is null || entry.Faction == query.Faction)
            .ToArray();
    }

    public static IReadOnlyList<SandboxSpawnAuthoringEntry> ListForContext(
        SandboxDeveloperContext context,
        SandboxSpawnAuthoringQuery? query = null)
    {
        if (!context.CanSpawnCurrentFaction)
        {
            return [];
        }

        return List(query)
            .Where(entry => entry.Faction is null || entry.Faction == context.Faction)
            .ToArray();
    }

    public static bool CanSpawnForContext(SandboxDeveloperContext context, SandboxSpawnAuthoringEntry entry)
    {
        return context.CanSpawnCurrentFaction
            && (entry.Faction is null || entry.Faction == context.Faction);
    }

    public static bool TryGetEntry(string id, out SandboxSpawnAuthoringEntry entry)
    {
        entry = Entries.FirstOrDefault(candidate => string.Equals(candidate.Id, id, StringComparison.Ordinal))!;
        return entry is not null;
    }

    public static SandboxSpawnAuthoringEntry GetEntry(string id)
    {
        return TryGetEntry(id, out var entry)
            ? entry
            : throw new InvalidOperationException($"Sandbox spawn entry '{id}' is not registered.");
    }

    public static EntitySpec EntitySpecFor(string id)
    {
        return CachedSpecs.Value.TryGetValue(id, out var spec)
            ? spec
            : throw new InvalidOperationException($"Sandbox spawn spec '{id}' is not registered.");
    }

    public static SandboxSpawnRequest CreateRequest(
        string id,
        OwnerId ownerId,
        Vector2 position,
        float facing = 0)
    {
        if (!ownerId.IsValid)
        {
            throw new ArgumentOutOfRangeException(nameof(ownerId), ownerId, "Sandbox spawn requests require a valid owner.");
        }

        var entry = GetEntry(id);
        var spec = EntitySpecFor(id);
        return new SandboxSpawnRequest(
            entry,
            spec,
            ownerId,
            EntityTransform.At(position, facing));
    }

    public static bool TryCreateRequestForContext(
        string id,
        SandboxDeveloperContext context,
        Vector2 position,
        float facing,
        out SandboxSpawnRequest? request,
        out string status)
    {
        request = null;

        if (!context.OwnerId.IsValid)
        {
            status = "Sandbox spawn requests require a valid owner.";
            return false;
        }

        if (!context.CanSpawnCurrentFaction)
        {
            status = $"Sandbox faction '{context.FactionOption.Key}' is locked.";
            return false;
        }

        if (!TryGetEntry(id, out var entry))
        {
            status = $"Sandbox spawn entry '{id}' is not registered.";
            return false;
        }

        if (!CanSpawnForContext(context, entry))
        {
            status = $"Sandbox spawn entry '{id}' is not available for faction '{context.FactionOption.Key}'.";
            return false;
        }

        request = CreateRequest(id, context.OwnerId, position, facing);
        status = "Sandbox spawn request created.";
        return true;
    }

    public static SandboxSpawnRequest CreateRequest(
        string id,
        PlayerSlotId playerSlotId,
        Vector2 position,
        float facing = 0)
    {
        return CreateRequest(id, OwnerId.FromPlayerSlot(playerSlotId), position, facing);
    }

    private static IReadOnlyList<SandboxSpawnAuthoringEntry> BuildEntries()
    {
        return UnitEntries()
            .Concat(BuildEntriesFromSpecs())
            .OrderBy(entry => entry.EntityKind)
            .ThenBy(entry => entry.Category, StringComparer.Ordinal)
            .ThenBy(entry => entry.Faction?.ToString() ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(entry => entry.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyDictionary<string, EntitySpec> BuildSpecs()
    {
        return Entries.ToDictionary(
            entry => entry.Id,
            entry => entry.Source switch
            {
                SandboxSpawnAuthoringSource.UnitDesign => UnitDesignCatalog.Spec(entry.Id).ToEntitySpec(),
                SandboxSpawnAuthoringSource.BuildSpec => BuildSpecFromEntry(entry).ToEntitySpec(),
                _ => throw new ArgumentOutOfRangeException(nameof(entry.Source), entry.Source, null),
            },
            StringComparer.Ordinal);
    }

    private static IEnumerable<SandboxSpawnAuthoringEntry> UnitEntries()
    {
        foreach (var design in UnitDesignCatalog.Designs.Values)
        {
            var spec = design.ToSpec();
            var entitySpec = spec.ToEntitySpec();
            yield return new SandboxSpawnAuthoringEntry(
                spec.Id,
                SandboxSpawnAuthoringSource.UnitDesign,
                EntityKind.Unit,
                spec.Production?.Category.ToString() ?? spec.Archetype.ToString(),
                spec.Faction,
                spec.Label,
                spec.NameKey,
                spec.ShortCode,
                spec.Icon,
                spec.Stats.TechTier,
                spec.Stats.Cost,
                entitySpec.Tags);
        }
    }

    private static IEnumerable<SandboxSpawnAuthoringEntry> BuildEntriesFromSpecs()
    {
        foreach (var spec in BuildSpecCatalog.Definitions.Values)
        {
            var entitySpec = spec.ToEntitySpec();
            yield return new SandboxSpawnAuthoringEntry(
                spec.EntitySpecId,
                SandboxSpawnAuthoringSource.BuildSpec,
                entitySpec.Kind,
                spec.Category.ToString(),
                null,
                spec.Label,
                spec.NameKey,
                spec.ShortCode,
                spec.Icon,
                entitySpec.Authoring.TechTier,
                spec.Cost,
                entitySpec.Tags);
        }
    }

    private static BuildSpec BuildSpecFromEntry(SandboxSpawnAuthoringEntry entry)
    {
        return BuildSpecCatalog.Definitions.Values.FirstOrDefault(spec => string.Equals(spec.EntitySpecId, entry.Id, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Sandbox build entry '{entry.Id}' does not map to a BuildSpec EntitySpecId.");
    }
}
