using System.Collections.Generic;
using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private UnitInstance AdoptUnitEntity(EntityInstance entity)
    {
        if (UnitByEntityId(entity.Id) is { } existing)
        {
            return existing;
        }

        var spec = UnitDesignCatalog.Spec(entity.SpecId);
        var unit = new UnitInstance
        {
            Id = _nextUnitId++,
            EntityId = entity.Id,
            Spec = spec,
            PlayerSlotId = entity.OwnerId.ToPlayerSlot(),
            Position = entity.Transform.Position,
            Facing = entity.Transform.Facing,
            Velocity = entity.Components.TryGet<MovementComponentState>(out var movement) ? movement.Velocity : Vector2.Zero,
            Hp = entity.Components.TryGet<HealthComponentState>(out var health) ? health.Hp : spec.Stats.MaxHp,
            Selected = entity.Components.TryGet<SelectableComponentState>(out var selectable) && selectable.Selected,
            PlayerIntentTarget = entity.Components.TryGet<CommandableComponentState>(out var commandable) ? commandable.PlayerIntentTarget : null,
            FormationSlot = movement?.FormationSlot,
            CommandVisualTarget = commandable?.CommandVisualTarget,
            MoveTarget = movement?.MoveTarget,
            MoveMode = commandable?.MoveMode ?? MoveCommandMode.Direct,
            Stance = entity.Components.TryGet<StanceComponentState>(out var stance) ? stance.Stance : spec.Weapons.Count > 0 ? UnitStance.Aggressive : UnitStance.Ignore,
            HarvesterMode = entity.Components.TryGet<HarvesterComponentState>(out var harvester) ? harvester.Mode : HarvesterMode.Idle,
            HarvestFieldId = harvester is null ? null : LegacyResourceFieldId(harvester.FieldId),
            HarvestRefineryId = harvester is null ? null : LegacyBuildingTargetId(harvester.RefineryId),
            HarvestPulse = harvester?.HarvestPulse ?? 0,
            Cargo = entity.Components.TryGet<ResourceCargoComponentState>(out var cargo) ? cargo.Cargo : 0,
        };

        FillAdoptedWeaponMounts(unit.WeaponMounts, entity, spec);
        Units.Add(unit);
        return unit;
    }

    private static void FillAdoptedWeaponMounts(List<WeaponMountRuntimeState> result, EntityInstance entity, UnitSpec spec)
    {
        result.Clear();
        if (entity.Components.TryGet<WeaponUserComponentState>(out var weapon))
        {
            for (var index = 0; index < weapon.Mounts.Count; index++)
            {
                result.Add(weapon.Mounts[index]);
            }

            return;
        }

        FillDefaultWeaponMounts(result, spec, entity.Transform.Facing);
    }

    private static void FillDefaultWeaponMounts(List<WeaponMountRuntimeState> result, UnitSpec spec, float facing)
    {
        for (var index = 0; index < spec.Weapons.Count; index++)
        {
            var mount = spec.Weapons[index];
            result.Add(new WeaponMountRuntimeState(mount.MountId, mount.WeaponId, facing, 0, mount.LegacyWeaponKind));
        }
    }
}
