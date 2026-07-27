using Godot;
using ProceduralRts.Core;

var failures = new List<string>();
var world = new EntityWorld(seed: 585);
var entity = world.Spawn(
    new EntitySpec
    {
        Id = "qa.unit-presentation",
        Kind = EntityKind.Unit,
        Display = new EntityDisplaySpec("Projection QA", "qa.unit.name", "qa.unit.role", "UPQ", IconGlyph.Infantry),
    },
    OwnerId.FromPlayerSlot(PlayerSlotId.One),
    EntityTransform.At(new Vector2(120, 340), 0.6f),
    [
        new HealthComponentState(88, 120),
        new SelectableComponentState(true, 0.25f),
        new MovementComponentState(new Vector2(12, -3), new Vector2(420, 540)),
        new PresentationPulseComponentState(CommandPulse: 0.8f, AlertPulse: 0.6f, HitPulse: 0.4f),
        new HarvesterComponentState(HarvesterMode.MovingToField, default, default, HarvestPulse: 0.7f),
        new ResourceCargoComponentState(320, 700),
        new WeaponUserComponentState([new WeaponMountRuntimeState("main", WeaponIds.VectorCannon, 1.2f, 0.3f)]),
    ]);

var first = UnitPresentationProjector.ProjectOne(world, entity);
var second = UnitPresentationProjector.ProjectOne(world, entity);
Require(first == second, "unchanged runtime presentation projection must be deterministic", failures);
Require(first.Entity.Id == entity.Id && first.Entity.Position == new Vector2(120, 340) && first.Entity.Facing == 0.6f,
    "projection must preserve immutable entity identity and transform", failures);
Require(first.Entity.Selected && first.Entity.Hp == 88 && first.Entity.MaxHp == 120,
    "projection must preserve entity selection and health", failures);
Require(first.Velocity == new Vector2(12, -3) && first.MoveTarget == new Vector2(420, 540) && first.IsMoving,
    "projection must preserve movement feedback without reading UnitInstance", failures);
Require(first.CommandPulse == 0.8f && first.AlertPulse == 0.6f && first.HitPulse == 0.4f,
    "projection must preserve presentation pulses and use the stronger alert pulse", failures);
Require(first.HarvestPulse == 0.7f && first.Cargo == 320,
    "projection must preserve economy feedback", failures);
Require(first.Mounts.Count == 1 && first.Mounts[0].MountId == "main" && first.Mounts[0].Facing == 1.2f,
    "projection must expose read-only mount-facing data", failures);

entity.Components.Set(new SelectableComponentState(false, 0.9f));
entity.Components.Set(new PresentationPulseComponentState(CommandPulse: 0.1f, AlertPulse: 0.4f, HitPulse: 0));
entity.Components.Set(new MovementComponentState(Vector2.Zero));
entity.Components.Set(new HarvesterComponentState(HarvesterMode.Idle, default, default, HarvestPulse: 0));
entity.Components.Set(new ResourceCargoComponentState(0, 700));
var updated = UnitPresentationProjector.ProjectOne(world, entity);
Require(!updated.Entity.Selected && updated.AlertPulse == 0.9f && updated.CommandPulse == 0.1f,
    "projection must replace stale selection and feedback state", failures);
Require(updated.Cargo == 0 && updated.HarvestPulse == 0 && !updated.IsMoving,
    "projection must clear stale economy and movement feedback", failures);

UnitPresentationProjectionRuntimeScenarios.Run(failures);

if (failures.Count > 0)
{
    Console.Error.WriteLine("UnitPresentationProjectionQa FAILED");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    System.Environment.Exit(1);
}

Console.WriteLine("UnitPresentationProjectionQa PASSED: immutable runtime feedback projection is deterministic and clears stale state.");

static void Require(bool condition, string message, List<string> failures)
{
    if (!condition)
    {
        failures.Add(message);
    }
}
