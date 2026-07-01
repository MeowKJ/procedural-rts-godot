using Godot;

namespace ProceduralRts.Core;

public sealed record SandboxStressSpawnPlan(
    SandboxDeveloperContext Context,
    IReadOnlyList<SandboxSpawnRequest> Requests,
    IReadOnlyList<string> Rejections)
{
    public int UnitCount => Requests.Count(request => request.Spec.Kind == EntityKind.Unit);
    public int StructureCount => Requests.Count(request => request.Spec.Kind is EntityKind.Building or EntityKind.Turret);
    public int BuildingCount => Requests.Count(request => request.Spec.Kind == EntityKind.Building);
    public int TurretCount => Requests.Count(request => request.Spec.Kind == EntityKind.Turret);
    public bool HasRequests => Requests.Count > 0;

    public string FormatStatus()
    {
        return HasRequests
            ? $"Sandbox stress: {UnitCount} units, {BuildingCount} buildings, {TurretCount} turrets"
            : Rejections.Count > 0 ? Rejections[0] : "Sandbox stress: no spawnable entries";
    }
}

public static class SandboxStressSpawnPlanner
{
    public const int MaxUnitRequests = 12;
    public const int MaxBuildingRequests = 4;
    public const int MaxTurretRequests = 2;

    public static SandboxStressSpawnPlan Create(
        SandboxDeveloperContext context,
        Vector2 center,
        float facing = 0)
    {
        if (!context.CanSpawnCurrentFaction)
        {
            return new SandboxStressSpawnPlan(
                context,
                [],
                [$"Sandbox stress: faction '{context.FactionOption.Key}' is locked."]);
        }

        var entries = SandboxSpawnAuthoring.ListForContext(context);
        var requests = new List<SandboxSpawnRequest>();
        var rejections = new List<string>();

        AddRequests(
            context,
            StressUnits(entries),
            center,
            facing,
            new Vector2(-132, -84),
            52,
            4,
            requests,
            rejections);
        AddRequests(
            context,
            StressStructures(entries, EntityKind.Building),
            center,
            facing,
            new Vector2(178, -118),
            178,
            2,
            requests,
            rejections);
        AddRequests(
            context,
            StressStructures(entries, EntityKind.Turret),
            center,
            facing,
            new Vector2(178, 112),
            148,
            2,
            requests,
            rejections);

        if (requests.Count == 0 && rejections.Count == 0)
        {
            rejections.Add("Sandbox stress: no spawnable entries for current context.");
        }

        return new SandboxStressSpawnPlan(context, requests, rejections);
    }

    private static IReadOnlyList<SandboxSpawnAuthoringEntry> StressUnits(IReadOnlyList<SandboxSpawnAuthoringEntry> entries)
    {
        return entries
            .Where(entry => entry.EntityKind == EntityKind.Unit)
            .OrderBy(entry => entry.TechTier)
            .ThenBy(entry => entry.Category, StringComparer.Ordinal)
            .ThenBy(entry => entry.Id, StringComparer.Ordinal)
            .Take(MaxUnitRequests)
            .ToArray();
    }

    private static IReadOnlyList<SandboxSpawnAuthoringEntry> StressStructures(
        IReadOnlyList<SandboxSpawnAuthoringEntry> entries,
        EntityKind kind)
    {
        var limit = kind == EntityKind.Turret ? MaxTurretRequests : MaxBuildingRequests;
        return entries
            .Where(entry => entry.EntityKind == kind)
            .OrderBy(entry => entry.TechTier)
            .ThenBy(entry => entry.Cost)
            .ThenBy(entry => entry.Id, StringComparer.Ordinal)
            .Take(limit)
            .ToArray();
    }

    private static void AddRequests(
        SandboxDeveloperContext context,
        IReadOnlyList<SandboxSpawnAuthoringEntry> entries,
        Vector2 center,
        float facing,
        Vector2 origin,
        float spacing,
        int columns,
        List<SandboxSpawnRequest> requests,
        List<string> rejections)
    {
        for (var index = 0; index < entries.Count; index++)
        {
            var column = index % columns;
            var row = index / columns;
            var offset = origin + new Vector2(column * spacing, row * spacing);
            var position = center + offset.Rotated(facing);
            if (SandboxSpawnAuthoring.TryCreateRequestForContext(
                    entries[index].Id,
                    context,
                    position,
                    facing,
                    out var request,
                    out var status)
                && request is not null)
            {
                requests.Add(request);
            }
            else
            {
                rejections.Add(status);
            }
        }
    }
}
