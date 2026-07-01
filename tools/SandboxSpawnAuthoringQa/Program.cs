using Godot;
using ProceduralRts.Core;

static void Fail(string message)
{
    throw new InvalidOperationException(message);
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        Fail(message);
    }
}

static void RequireStableOrder(IReadOnlyList<SandboxSpawnAuthoringEntry> entries)
{
    var sorted = entries
        .OrderBy(entry => entry.EntityKind)
        .ThenBy(entry => entry.Category, StringComparer.Ordinal)
        .ThenBy(entry => entry.Faction?.ToString() ?? string.Empty, StringComparer.Ordinal)
        .ThenBy(entry => entry.Id, StringComparer.Ordinal)
        .Select(entry => entry.Id)
        .ToArray();
    var actual = entries.Select(entry => entry.Id).ToArray();

    if (!actual.SequenceEqual(sorted))
    {
        Fail("Sandbox spawn entries must stay in deterministic display order.");
    }
}

static void RequireEntryShape(SandboxSpawnAuthoringEntry entry)
{
    Require(!string.IsNullOrWhiteSpace(entry.Id), "entry id must be present");
    Require(!string.IsNullOrWhiteSpace(entry.Label), $"{entry.Id} label must be present");
    Require(!string.IsNullOrWhiteSpace(entry.NameKey), $"{entry.Id} name key must be present");
    Require(!string.IsNullOrWhiteSpace(entry.ShortCode), $"{entry.Id} short code must be present");
    Require(!string.IsNullOrWhiteSpace(entry.Category), $"{entry.Id} category must be present");
    Require(entry.Tags.Count > 0, $"{entry.Id} must expose authoring tags");

    var spec = SandboxSpawnAuthoring.EntitySpecFor(entry.Id);
    Require(spec.Id == entry.Id, $"{entry.Id} EntitySpec id must round-trip");
    Require(spec.Kind == entry.EntityKind, $"{entry.Id} EntitySpec kind must match display entry");
    Require(spec.Display.Label == entry.Label, $"{entry.Id} display label must match EntitySpec");
}

static SandboxDeveloperContext ApplyParsed(SandboxDeveloperContext context, string field, string value)
{
    Require(
        SandboxDeveloperContextOptions.TryParseRequest(field, value, out var request),
        $"sandbox developer context must parse {field}={value}");
    return context.Apply(request);
}

static void RequireStressPlan(SandboxDeveloperContext context, Vector2 center)
{
    var plan = SandboxStressSpawnPlanner.Create(context, center);
    Require(plan.HasRequests, "sandbox stress planner must create requests for playable contexts.");
    Require(plan.UnitCount > 0, "sandbox stress planner must include unit requests.");
    Require(plan.BuildingCount > 0, "sandbox stress planner must include building requests.");
    Require(plan.TurretCount > 0, "sandbox stress planner must include turret requests.");
    Require(plan.Requests.All(request => request.OwnerId == context.OwnerId), "sandbox stress planner must preserve the selected owner.");
    Require(plan.Requests.All(request => request.Transform.Position.X > 0 && request.Transform.Position.Y > 0), "sandbox stress planner must provide positive spawn positions.");
    Require(plan.Requests
        .Where(request => request.Entry.Faction is not null)
        .All(request => request.Entry.Faction == context.Faction), "sandbox stress planner must keep faction-specific unit requests inside the selected faction.");
    Require(plan.FormatStatus().Contains("Sandbox stress", StringComparison.Ordinal), "sandbox stress planner must expose a HUD-safe status.");
}

static void RequireLockedStressPlan(SandboxDeveloperContext context)
{
    var plan = SandboxStressSpawnPlanner.Create(context, new Vector2(240, 240));
    Require(!plan.HasRequests, "locked sandbox stress planner must not create requests.");
    Require(plan.Rejections.Count > 0, "locked sandbox stress planner must explain why no requests were created.");
    Require(plan.FormatStatus().Contains("locked", StringComparison.OrdinalIgnoreCase), "locked sandbox stress status must mention the lock.");
}

var entries = SandboxSpawnAuthoring.Entries;
Require(entries.Count > 0, "Sandbox spawn authoring must expose entries.");
RequireStableOrder(entries);

foreach (var entry in entries)
{
    RequireEntryShape(entry);
}

var specs = entries.Select(entry => SandboxSpawnAuthoring.EntitySpecFor(entry.Id)).ToArray();
Require(specs.Length == entries.Count, "Sandbox spawn authoring must expose an EntitySpec for every entry.");
Require(specs.Select(spec => spec.Id).SequenceEqual(entries.Select(entry => entry.Id)), "Sandbox spawn specs must follow entry order.");

var unitEntries = SandboxSpawnAuthoring.List(new SandboxSpawnAuthoringQuery(EntityKind.Unit));
Require(unitEntries.Count > 0, "Sandbox spawn authoring must expose unit entries.");
Require(unitEntries.All(entry => entry.Source == SandboxSpawnAuthoringSource.UnitDesign), "unit entries must come from UnitDesign.");
Require(unitEntries.Any(entry => entry.Faction == UnitFactionId.Dog), "unit entries must include Dog faction.");
Require(unitEntries.Any(entry => entry.Faction == UnitFactionId.Cat), "unit entries must include Cat faction.");

var dogInfantry = SandboxSpawnAuthoring.List(new SandboxSpawnAuthoringQuery(
    EntityKind: EntityKind.Unit,
    Category: ProductionCategory.Infantry.ToString(),
    Faction: UnitFactionId.Dog));
Require(dogInfantry.Count > 0, "Sandbox spawn authoring must filter Dog infantry unit designs.");
Require(dogInfantry.All(entry => entry.EntityKind == EntityKind.Unit
    && entry.Category == ProductionCategory.Infantry.ToString()
    && entry.Faction == UnitFactionId.Dog), "Dog infantry filter must only return matching entries.");

var buildingEntries = SandboxSpawnAuthoring.List(new SandboxSpawnAuthoringQuery(EntityKind.Building));
var turretEntries = SandboxSpawnAuthoring.List(new SandboxSpawnAuthoringQuery(EntityKind.Turret));
Require(buildingEntries.Count > 0, "Sandbox spawn authoring must expose building BuildSpec entries.");
Require(turretEntries.Count > 0, "Sandbox spawn authoring must expose turret BuildSpec entries.");
Require(buildingEntries.Concat(turretEntries).All(entry => entry.Source == SandboxSpawnAuthoringSource.BuildSpec), "structure entries must come from BuildSpec.");

var kinds = SandboxSpawnAuthoring.EntityKinds;
Require(kinds.Contains(EntityKind.Unit), "authoring kind list must include Unit.");
Require(kinds.Contains(EntityKind.Building), "authoring kind list must include Building.");
Require(kinds.Contains(EntityKind.Turret), "authoring kind list must include Turret.");

var context = SandboxDeveloperContext.Default;
Require(context.OwnerId == OwnerId.FromPlayerSlot(PlayerSlotId.One), "default sandbox context must start on owner 1.");
Require(context.Faction == UnitFactionId.Dog, "default sandbox context must start on Dog faction.");
Require(context.CanSpawnCurrentFaction, "default Dog sandbox context must be spawnable.");

var dogContextEntries = SandboxSpawnAuthoring.ListForContext(context);
Require(dogContextEntries.Any(entry => entry.Faction == UnitFactionId.Dog), "Dog context must expose Dog unit entries.");
Require(dogContextEntries.Any(entry => entry.EntityKind == EntityKind.Building), "Dog context must expose building entries.");
Require(dogContextEntries.Any(entry => entry.EntityKind == EntityKind.Turret), "Dog context must expose turret entries.");
Require(!dogContextEntries.Any(entry => entry.Faction == UnitFactionId.Cat), "Dog context must not expose Cat unit entries.");
RequireStressPlan(context, new Vector2(900, 900));

context = ApplyParsed(context, "owner", "owner-2");
context = ApplyParsed(context, "faction", "cat");
context = ApplyParsed(context, "team", "team-3");
context = ApplyParsed(context, "relation", "allied");
context = ApplyParsed(context, "environment", "corruption");
context = ApplyParsed(context, "time-scale", "2x");
context = ApplyParsed(context, "debug-overlay", "movement");
context = ApplyParsed(context, "debug-overlay-toggle", "state-hash");

Require(context.OwnerId == OwnerId.FromPlayerSlot(PlayerSlotId.Two), "sandbox context must switch owner.");
Require(context.Faction == UnitFactionId.Cat, "sandbox context must switch faction.");
Require(context.TeamId == 3, "sandbox context must switch team.");
Require(context.Relation == PlayerRelation.Allied, "sandbox context must switch relation.");
Require(context.Environment == SandboxAtmospherePreset.Corruption, "sandbox context must switch environment.");
Require(MathF.Abs(context.TimeScale - 2f) < 0.0001f, "sandbox context must switch time preset.");
Require(context.DebugOverlay.IsEnabled(SandboxDebugOverlayFlag.Paths), "sandbox context must apply debug overlay preset.");
Require(context.DebugOverlay.IsEnabled(SandboxDebugOverlayFlag.StateHash), "sandbox context must toggle debug overlay flags.");
Require(context.FormatStatus().Contains("owner-2", StringComparison.Ordinal), "sandbox context status must include owner key.");

var catContextEntries = SandboxSpawnAuthoring.ListForContext(context);
Require(catContextEntries.Any(entry => entry.Faction == UnitFactionId.Cat), "Cat context must expose Cat unit entries.");
Require(catContextEntries.Any(entry => entry.EntityKind == EntityKind.Building), "Cat context must expose building entries.");
Require(catContextEntries.Any(entry => entry.EntityKind == EntityKind.Turret), "Cat context must expose turret entries.");
Require(!catContextEntries.Any(entry => entry.Faction == UnitFactionId.Dog), "Cat context must not expose Dog unit entries.");
RequireStressPlan(context, new Vector2(1200, 900));

Require(
    SandboxSpawnAuthoring.TryCreateRequestForContext(catContextEntries.First(entry => entry.Faction == UnitFactionId.Cat).Id, context, new Vector2(8, 9), 0.1f, out var catContextRequest, out var catStatus),
    $"Cat context request must be accepted: {catStatus}");
Require(catContextRequest is not null, "accepted context request must return a request.");
var acceptedCatContextRequest = catContextRequest!;
Require(acceptedCatContextRequest.OwnerId == OwnerId.FromPlayerSlot(PlayerSlotId.Two), "context request must preserve switched owner.");
Require(acceptedCatContextRequest.Entry.Faction == UnitFactionId.Cat, "context request must preserve switched faction filter.");

Require(
    !SandboxSpawnAuthoring.TryCreateRequestForContext(dogInfantry[0].Id, context, new Vector2(8, 9), 0, out var wrongFactionRequest, out _),
    "Cat context must reject Dog unit requests.");
Require(wrongFactionRequest is null, "rejected wrong-faction request must not return a request.");

context = ApplyParsed(context, "faction", "corruption");
Require(context.Faction == UnitFactionId.Corruption, "sandbox context must parse the third faction.");
Require(!context.CanSpawnCurrentFaction, "Corruption must remain a locked placeholder.");
Require(context.FactionOption.Availability == SandboxFactionAvailability.LockedPlaceholder, "Corruption option must be marked locked.");
Require(context.FactionOption.LockedReasonKey == "faction.corruption.locked", "Corruption lock reason key must stay stable.");
Require(SandboxSpawnAuthoring.List(new SandboxSpawnAuthoringQuery(EntityKind: EntityKind.Unit, Faction: UnitFactionId.Corruption)).Count == 0, "Corruption must not have unit spawn entries yet.");
Require(SandboxSpawnAuthoring.ListForContext(context).Count == 0, "locked Corruption context must not expose spawnable content.");
Require(
    !SandboxSpawnAuthoring.TryCreateRequestForContext("building.powerplant", context, new Vector2(4, 5), 0, out var lockedRequest, out var lockedStatus),
    $"locked Corruption context must reject spawn requests: {lockedStatus}");
Require(lockedRequest is null, "locked Corruption request must not return a request.");
RequireLockedStressPlan(context);

var dogRequest = SandboxSpawnAuthoring.CreateRequest(
    dogInfantry[0].Id,
    PlayerSlotId.One,
    new Vector2(12, 34),
    0.75f);
Require(dogRequest.Entry.Id == dogInfantry[0].Id, "spawn request must preserve entry id.");
Require(dogRequest.OwnerId == OwnerId.FromPlayerSlot(PlayerSlotId.One), "spawn request must preserve owner.");
Require(dogRequest.Transform.Position == new Vector2(12, 34), "spawn request must preserve position.");
Require(MathF.Abs(dogRequest.Transform.Facing - 0.75f) < 0.0001f, "spawn request must preserve facing.");
Require(dogRequest.Spec.Kind == EntityKind.Unit, "unit spawn request must carry a unit EntitySpec.");

var powerPlantRequest = SandboxSpawnAuthoring.CreateRequest(
    "building.powerplant",
    new OwnerId(2),
    new Vector2(100, 200));
Require(powerPlantRequest.Spec.Kind == EntityKind.Building, "building spawn request must carry a building EntitySpec.");
Require(powerPlantRequest.OwnerId.Value == 2, "building spawn request must preserve numeric owner.");

Console.WriteLine(
    $"SandboxSpawnAuthoringQa PASSED: entries {entries.Count}, specs {specs.Length}, units {unitEntries.Count}, buildings {buildingEntries.Count}, turrets {turretEntries.Count}, context switches covered.");
