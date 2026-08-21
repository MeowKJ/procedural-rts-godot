using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private void NotifyCreditChanges(IReadOnlyDictionary<PlayerSlotId, int> creditsBefore)
    {
        CollectResourceCreditOwnerIds(_resourceCreditOwnerIds);
        foreach (var ownerValue in _resourceCreditOwnerIds)
        {
            var owner = new OwnerId(ownerValue);
            var playerSlotId = owner.ToPlayerSlot();
            var inventory = ResourceInventory(playerSlotId);
            if (!creditsBefore.TryGetValue(playerSlotId, out var before) || before != inventory.Credits)
            {
                ResourceInventoryChanged?.Invoke(playerSlotId, inventory);
            }
        }
    }

    private void CollectResourceCreditOwnerIds(List<int> result)
    {
        result.Clear();
        foreach (var ownerValue in _entityWorld.ResourceInventories.Keys)
        {
            AddResourceCreditOwnerId(result, ownerValue);
        }

        foreach (var entity in _entityWorld.OrderedEntities)
        {
            AddResourceCreditOwnerId(result, entity.OwnerId.Value);
        }

        result.Sort();
    }

    private static void AddResourceCreditOwnerId(List<int> result, int ownerValue)
    {
        if (ownerValue <= 0 || result.Contains(ownerValue))
        {
            return;
        }

        result.Add(ownerValue);
    }

    private static void SyncBodyFixedMountFacings(UnitInstance unit)
    {
        for (var index = 0; index < unit.WeaponMounts.Count && index < unit.Spec.Weapons.Count; index++)
        {
            var spec = unit.Spec.Weapons[index];
            if (spec.FacingMode != WeaponMountFacingMode.Independent)
            {
                unit.MutableWeaponMounts[index] = unit.WeaponMounts[index] with { Facing = unit.Facing };
            }
        }
    }

    private UnitInstance? UnitById(int id)
    {
        return Units.FirstOrDefault(unit => unit.Id == id);
    }

    private static WeaponDefinition PrimaryWeapon(UnitInstance unit)
    {
        return WeaponCatalog.WeaponDefinitions[unit.Spec.PrimaryWeapon.WeaponId];
    }

    private void ClearEntityAttackTarget(UnitInstance unit)
    {
        if (!_entityWorld.TryGet(unit.EntityId, out var entity)
            || !entity.Components.TryGet<WeaponUserComponentState>(out var weapon))
        {
            return;
        }

        entity.Components.Set(weapon with
        {
            AttackTarget = EntityId.None,
            AttackTargetKind = CombatTargetKind.Unit,
            AttackTargetIsManual = false,
        });
        RefreshUnitProjection(unit, entity);
    }

    private void ClearBuildingAttackTargetCore(int buildingId)
    {
        if (!_buildingTargetEntityIds.TryGetValue(buildingId, out var entityId)
            || !_entityWorld.TryGet(entityId, out var entity)
            || !entity.Components.TryGet<WeaponUserComponentState>(out var weapon))
        {
            return;
        }

        entity.Components.Set(weapon with
        {
            AttackTarget = EntityId.None,
            AttackTargetKind = CombatTargetKind.Unit,
            AttackTargetIsManual = false,
        });
    }

    private static bool UnitOverlapsSelectionRect(Rect2 worldRect, UnitInstance unit)
    {
        if (worldRect.HasPoint(unit.Position))
        {
            return true;
        }

        var radius = unit.Spec.Collision.Radius * 0.72f;
        var closest = new Vector2(
            Mathf.Clamp(unit.Position.X, worldRect.Position.X, worldRect.End.X),
            Mathf.Clamp(unit.Position.Y, worldRect.Position.Y, worldRect.End.Y));
        return closest.DistanceSquaredTo(unit.Position) <= radius * radius;
    }

    private static bool ShouldIncludeEconomyInSelectionRect(
        Rect2 worldRect,
        IReadOnlyList<UnitInstance> economyUnits,
        IReadOnlyList<UnitInstance> nonEconomyUnits)
    {
        if (economyUnits.Count == 0)
        {
            return false;
        }

        if (nonEconomyUnits.Count == 0 || economyUnits.Count > nonEconomyUnits.Count)
        {
            return true;
        }

        var rectSize = worldRect.Size;
        var maxSide = Mathf.Max(Mathf.Abs(rectSize.X), Mathf.Abs(rectSize.Y));
        if (maxSide > SelectionHarvesterIntentMaxSize)
        {
            return false;
        }

        var center = worldRect.Position + worldRect.Size / 2f;
        var nearestEconomy = float.PositiveInfinity;
        foreach (var unit in economyUnits)
        {
            nearestEconomy = MathF.Min(nearestEconomy, unit.Position.DistanceTo(center));
        }

        var nearestNonEconomy = float.PositiveInfinity;
        foreach (var unit in nonEconomyUnits)
        {
            nearestNonEconomy = MathF.Min(nearestNonEconomy, unit.Position.DistanceTo(center));
        }

        return nearestEconomy <= nearestNonEconomy + SelectionEconomyIntentCenterMargin;
    }

}
