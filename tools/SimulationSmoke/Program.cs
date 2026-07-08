using Godot;
using ProceduralRts.Core;
using ProceduralRts.World;

const float DurationSeconds = 300f;
const float StepSeconds = 0.1f;

static void Fail(string message)
{
    throw new InvalidOperationException(message);
}

static void Expect(bool condition, string message)
{
    if (!condition)
    {
        Fail(message);
    }
}

static bool IsFinite(Vector2 value)
{
    return float.IsFinite(value.X) && float.IsFinite(value.Y);
}

static void ValidateSandboxDebugOverlayState()
{
    var state = SandboxDebugOverlayState.Empty;
    Expect(state.FormatStatus() == "Sandbox overlays: off", "empty sandbox overlay status should be stable");

    state = state.Toggle(SandboxDebugOverlayFlag.Paths);
    Expect(state.IsEnabled(SandboxDebugOverlayFlag.Paths), "paths overlay should toggle on");
    Expect(SandboxDebugOverlayState.FormatLabel(SandboxDebugOverlayFlag.Paths) == "Paths", "paths overlay label should be stable");
    Expect(state.FormatStatus(SandboxDebugOverlayFlag.Paths) == "Paths: on", "single overlay status should be stable");

    state = state.Set(SandboxDebugOverlayFlag.Paths, false);
    Expect(!state.IsEnabled(SandboxDebugOverlayFlag.Paths), "paths overlay should set off");

    var movementPreset = SandboxDebugOverlayState.PresetByKey("movement");
    state = state.ApplyPreset(movementPreset);
    Expect(state.IsEnabled(SandboxDebugOverlayFlag.Paths | SandboxDebugOverlayFlag.Slots | SandboxDebugOverlayFlag.Avoidance),
        "movement preset should enable paths, slots, and avoidance");
    Expect(state.IsEnabled(SandboxDebugOverlayFlag.Rings | SandboxDebugOverlayFlag.Anchors),
        "movement preset should enable rings and anchors");
    Expect(!state.IsEnabled(SandboxDebugOverlayFlag.Components | SandboxDebugOverlayFlag.CommandLog | SandboxDebugOverlayFlag.StateHash),
        "movement preset should not enable diagnostics overlays");

    var allEnabled = state.Set(SandboxDebugOverlayFlag.All, true);
    Expect(allEnabled.IsEnabled(SandboxDebugOverlayFlag.All), "all overlay flag should enable every known overlay");
    Expect(allEnabled.FormatStatus().Contains("state-hash", StringComparison.Ordinal), "all overlay status should include state hash");

    Expect(!PathDebugLayer.RuntimeSandboxOverlaysVisible(LaunchMode.Skirmish, SandboxDebugOverlayFlag.Paths),
        "sandbox runtime overlays must stay hidden outside Sandbox launch mode");
    Expect(!PathDebugLayer.RuntimeSandboxOverlaysVisible(LaunchMode.Sandbox, SandboxDebugOverlayFlag.StateHash),
        "diagnostic-only sandbox flags must not activate path debug drawing");
    Expect(PathDebugLayer.RuntimeSandboxOverlaysVisible(LaunchMode.Sandbox, SandboxDebugOverlayFlag.Paths),
        "Sandbox Paths flag should activate runtime path debug drawing");
    Expect(PathDebugLayer.RuntimeSandboxOverlaysVisible(LaunchMode.Sandbox, SandboxDebugOverlayFlag.Rings),
        "Sandbox Rings flag should activate runtime radius debug drawing");
    Expect(PathDebugLayer.RuntimeSandboxOverlaysVisible(LaunchMode.Sandbox, SandboxDebugOverlayFlag.Anchors),
        "Sandbox Anchors flag should activate runtime anchor debug drawing");
    Expect(PathDebugLayer.RuntimeSandboxOverlaysVisible(LaunchMode.Sandbox, movementPreset.Flags),
        "Sandbox movement preset should activate runtime movement overlays");
    Expect(!PathDebugLayer.RuntimeSandboxOverlaysVisible(LaunchMode.Sandbox, SandboxDebugOverlayState.PresetByKey("diagnostics").Flags),
        "Sandbox diagnostics preset should not activate runtime path/ring/anchor drawing");
}

static void AssignHarvester(GameState state, Owner owner, ResourceFieldModel field)
{
    var harvester = state.Units
        .Where(unit => unit.Owner == owner)
        .FirstOrDefault(GameState.IsHarvesterUnit);
    if (harvester is null)
    {
        return;
    }

    state.ClearSelection();
    harvester.Selected = true;
    if (!state.CommandHarvestSelected(field, out var status))
    {
        Fail($"expected {owner} harvester assignment to succeed: {status}");
    }
}

static UnitSpecRuntimeDescriptor RuntimeDescriptorFor(UnitModel unit)
{
    return unit.RuntimeDescriptor;
}

static void ValidateState(GameState state, float elapsed)
{
    if (state.Credits(Owner.Player) < 0 || state.Credits(Owner.Enemy) < 0)
    {
        Fail($"credits should never become negative at {elapsed:0.0}s");
    }

    if (state.ResourceFields.Any(field => field.Amount < 0 || !IsFinite(field.Position)))
    {
        Fail($"resource fields should stay finite and non-negative at {elapsed:0.0}s");
    }

    var unitIds = state.Units.Select(unit => unit.Id).ToHashSet();
    var buildingIds = state.Buildings.Select(building => building.Id).ToHashSet();
    if (unitIds.Count != state.Units.Count || buildingIds.Count != state.Buildings.Count)
    {
        Fail($"entity ids should remain unique at {elapsed:0.0}s");
    }

    foreach (var unit in state.Units)
    {
        var descriptor = RuntimeDescriptorFor(unit);
        if (!IsFinite(unit.Position)
            || unit.Position.X < -descriptor.Radius
            || unit.Position.Y < -descriptor.Radius
            || unit.Position.X > state.WorldSize.X + descriptor.Radius
            || unit.Position.Y > state.WorldSize.Y + descriptor.Radius)
        {
            Fail($"unit {unit.Id} moved outside stable world bounds at {elapsed:0.0}s");
        }

        if (unit.Hp <= 0)
        {
            Fail($"dead unit {unit.Id} should have been removed by {elapsed:0.0}s");
        }

        if (unit.Cargo < 0 || unit.Cargo > GameState.HarvesterCargoCapacity)
        {
            Fail($"harvester cargo should remain bounded at {elapsed:0.0}s");
        }

        if (unit.AttackTargetId is not null)
        {
            var valid = unit.AttackTargetKind == CombatTargetKind.Unit
                ? unitIds.Contains(unit.AttackTargetId.Value)
                : buildingIds.Contains(unit.AttackTargetId.Value);
            if (!valid)
            {
                Fail($"unit {unit.Id} has stale attack target at {elapsed:0.0}s");
            }
        }

        if (unit.HarvestFieldId is not null && state.ResourceFieldById(unit.HarvestFieldId.Value) is null)
        {
            Fail($"unit {unit.Id} has stale harvest field at {elapsed:0.0}s");
        }

        if (unit.HarvestRefineryId is not null && state.BuildingById(unit.HarvestRefineryId.Value) is null)
        {
            Fail($"unit {unit.Id} has stale harvest refinery at {elapsed:0.0}s");
        }
    }

    foreach (var building in state.Buildings)
    {
        if (!IsFinite(building.Position) || building.Hp <= 0)
        {
            Fail($"building {building.Id} should stay finite and alive-or-removed at {elapsed:0.0}s");
        }

        if (building.AttackTargetId is not null)
        {
            var valid = building.AttackTargetKind == CombatTargetKind.Unit
                ? unitIds.Contains(building.AttackTargetId.Value)
                : buildingIds.Contains(building.AttackTargetId.Value);
            if (!valid)
            {
                Fail($"building {building.Id} has stale attack target at {elapsed:0.0}s");
            }
        }

        if (building.DockedHarvesterId is not null && !unitIds.Contains(building.DockedHarvesterId.Value))
        {
            Fail($"building {building.Id} has stale docked harvester at {elapsed:0.0}s");
        }

        if (building.DockReservedByHarvesterId is not null && !unitIds.Contains(building.DockReservedByHarvesterId.Value))
        {
            Fail($"building {building.Id} has stale reserved harvester at {elapsed:0.0}s");
        }

        foreach (var item in building.ProductionQueue)
        {
            var productionSpec = UnitDesignCatalog.Spec(item.DesignId).Production
                ?? throw new InvalidOperationException($"{item.DesignId} should resolve to a production-capable UnitSpec in SimulationSmoke");
            var durationLimit = productionSpec.Duration;
            if (item.Progress < 0
                || item.Progress > durationLimit + 0.001f
                || productionSpec.LaneIndex < 0
                || string.IsNullOrWhiteSpace(productionSpec.LaneKey))
            {
                Fail($"production queue item should remain valid at {elapsed:0.0}s");
            }
        }
    }

    foreach (var projectile in state.Projectiles)
    {
        var sourceValid = projectile.SourceKind == CombatSourceKind.Unit
            ? unitIds.Contains(projectile.SourceId)
            : buildingIds.Contains(projectile.SourceId);
        var targetValid = projectile.TargetKind == CombatTargetKind.Unit
            ? unitIds.Contains(projectile.TargetId)
            : buildingIds.Contains(projectile.TargetId);
        if (!sourceValid || !targetValid || !IsFinite(projectile.Position))
        {
            Fail($"projectile references should remain valid at {elapsed:0.0}s");
        }
    }

    foreach (var beam in state.Beams)
    {
        var sourceValid = beam.SourceKind == CombatSourceKind.Unit
            ? unitIds.Contains(beam.SourceId)
            : buildingIds.Contains(beam.SourceId);
        var targetValid = beam.TargetKind == CombatTargetKind.Unit
            ? unitIds.Contains(beam.TargetId)
            : buildingIds.Contains(beam.TargetId);
        if (!sourceValid || !targetValid || beam.Age < -0.001f || beam.Age > beam.Duration + 0.25f)
        {
            Fail($"beam references should remain valid at {elapsed:0.0}s");
        }
    }
}

ValidateSandboxDebugOverlayState();

var options = new SkirmishOptions(
    StartingCredits: 5000,
    MapSeed: 515151,
    EnemyDifficulty: EnemyDifficulty.Hard);
var state = new GameState(options);
var productionAi = new EnemyProductionAi(EnemyDifficultyProfile.For(options.EnemyDifficulty));
var waveAi = new EnemyAttackWaveAi(EnemyDifficultyProfile.For(options.EnemyDifficulty));

AssignHarvester(state, Owner.Player, state.ResourceFields.OrderBy(field => field.Position.DistanceSquaredTo(new Vector2(720, 760))).First());

var initialPlayerCredits = state.Credits(Owner.Player);
var initialEnemyCredits = state.Credits(Owner.Enemy);
var initialFieldAmounts = state.ResourceFields.Sum(field => field.Amount);
var productionEvents = 0;
var completedEvents = 0;
var enemyUnitsAdded = 0;
var resourceEvents = 0;

state.ProductionQueued += (_, _) => productionEvents++;
state.ProductionCompleted += (_, _) => completedEvents++;
state.UnitAdded += unit =>
{
    if (unit.Owner == Owner.Enemy)
    {
        enemyUnitsAdded++;
    }
};
state.ResourceInventoryChanged += (_, _) => resourceEvents++;

for (var elapsed = 0f; elapsed < DurationSeconds; elapsed += StepSeconds)
{
    productionAi.Update(state, StepSeconds);
    waveAi.Update(state, StepSeconds);
    state.Update(StepSeconds);

    if (((int)MathF.Round(elapsed * 10)) % 25 == 0)
    {
        ValidateState(state, elapsed);
    }
}

ValidateState(state, DurationSeconds);

var finalFieldAmounts = state.ResourceFields.Sum(field => field.Amount);
if (productionAi.SuccessfulOrders < 3 || productionEvents < 3)
{
    Fail("5-minute smoke should exercise enemy production orders");
}

if (completedEvents < 2 || enemyUnitsAdded < 2)
{
    Fail("5-minute smoke should complete production into new enemy units");
}

if (waveAi.WavesLaunched < 1)
{
    Fail("5-minute smoke should exercise enemy attack waves");
}

if (resourceEvents == 0 || finalFieldAmounts >= initialFieldAmounts)
{
    Fail("5-minute smoke should exercise harvesting and resource depletion");
}

if (state.Credits(Owner.Player) == initialPlayerCredits && state.Credits(Owner.Enemy) == initialEnemyCredits)
{
    Fail("5-minute smoke should change at least one resource inventory");
}

Console.WriteLine(
    $"Simulation smoke passed: {DurationSeconds:0}s, orders {productionAi.SuccessfulOrders}, completions {completedEvents}, waves {waveAi.WavesLaunched}, outcome {state.Outcome}");
