using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private void SyncAllCreditsFromEntityWorld(IReadOnlyDictionary<PlayerSlotId, int> creditsBefore)
    {
        CollectResourceCreditOwnerIds(_resourceCreditOwnerIds);
        foreach (var ownerValue in _resourceCreditOwnerIds)
        {
            var owner = new OwnerId(ownerValue);
            var playerSlotId = owner.ToPlayerSlot();
            SyncCreditsFromEntityWorld(playerSlotId);
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

    private EntityId AttackTargetEntityId(UnitInstance unit)
    {
        if (unit.AttackTargetId is not { } targetId)
        {
            return EntityId.None;
        }

        if (unit.AttackTargetKind == CombatTargetKind.Building)
        {
            return _buildingTargetEntityIds.TryGetValue(targetId, out var buildingEntityId)
                ? buildingEntityId
                : EntityId.None;
        }

        return UnitById(targetId)?.EntityId ?? EntityId.None;
    }

    private static IReadOnlyList<WeaponMountRuntimeState> WeaponMountsForEntity(UnitInstance unit)
    {
        var count = unit.WeaponMounts.Count;
        if (count == 0)
        {
            return [];
        }

        var copy = new WeaponMountRuntimeState[count];
        for (var index = 0; index < count; index++)
        {
            var mount = unit.WeaponMounts[index];
            copy[index] = mount with { CooldownRemaining = unit.AttackCooldownRemaining };
        }

        return copy;
    }

    private static void SyncBodyFixedMountFacings(UnitInstance unit)
    {
        for (var index = 0; index < unit.WeaponMounts.Count && index < unit.Spec.Weapons.Count; index++)
        {
            var spec = unit.Spec.Weapons[index];
            if (spec.FacingMode != WeaponMountFacingMode.Independent)
            {
                unit.WeaponMounts[index] = unit.WeaponMounts[index] with { Facing = unit.Facing };
            }
        }
    }

    private void ResolveSoftCollisions(float dt)
    {
        for (var a = 0; a < Units.Count; a++)
        {
            var first = Units[a];
            if (!first.Spec.Collision.BlocksMovement)
            {
                continue;
            }

            for (var b = a + 1; b < Units.Count; b++)
            {
                var second = Units[b];
                if (!second.Spec.Collision.BlocksMovement)
                {
                    continue;
                }

                ResolvePair(first, second, dt);
            }
        }
    }

    private static void ResolvePair(UnitInstance first, UnitInstance second, float dt)
    {
        var delta = second.Position - first.Position;
        var distance = delta.Length();
        var minDistance = first.Spec.Collision.Radius + second.Spec.Collision.Radius;
        if (distance >= minDistance || minDistance <= 0)
        {
            return;
        }

        var normal = distance <= 0.001f ? Vector2.Right : delta / distance;
        var overlap = minDistance - distance;
        var firstWeight = ResolveWeight(first);
        var secondWeight = ResolveWeight(second);
        var totalWeight = firstWeight + secondWeight;
        var firstShare = totalWeight <= 0 ? 0.5f : secondWeight / totalWeight;
        var secondShare = totalWeight <= 0 ? 0.5f : firstWeight / totalWeight;
        var settle = Mathf.Clamp(dt * 16f, 0.15f, 0.9f);

        first.Position -= normal * overlap * firstShare * settle;
        second.Position += normal * overlap * secondShare * settle;
    }

    private static float ResolveWeight(UnitInstance unit)
    {
        var movingBias = unit.IsMoving ? 1.25f : 0.42f;
        var priorityBias = 1f / MathF.Max(1f, unit.Spec.Collision.PushPriority);
        return movingBias * priorityBias / MathF.Max(unit.Spec.Collision.Mass, 0.1f);
    }

    private UnitInstance? UnitById(int id)
    {
        return Units.FirstOrDefault(unit => unit.Id == id);
    }

    private static WeaponDefinition PrimaryWeapon(UnitInstance unit)
    {
        return WeaponCatalog.Weapons[unit.Spec.PrimaryWeapon.WeaponKind];
    }

    private static bool CanUnitTarget(UnitInstance unit, UnitInstance target)
    {
        return CanWeaponTarget(PrimaryWeapon(unit), target.Spec);
    }

    private static bool CanUnitTarget(UnitInstance unit, BuildSpec targetSpec)
    {
        return CanWeaponTarget(PrimaryWeapon(unit), targetSpec);
    }

    private static bool CanWeaponTarget(WeaponDefinition weapon, UnitSpec target)
    {
        return weapon.TargetProfile.AllowedDomains.Contains(target.Movement.Domain)
            && weapon.TargetProfile.AllowedArmorTags.Contains(target.Stats.ArmorTag);
    }

    private static bool CanWeaponTarget(WeaponDefinition weapon, BuildSpec targetSpec)
    {
        return weapon.TargetProfile.CanTarget(targetSpec);
    }

    private static float WeaponTargetPriority(WeaponDefinition weapon, UnitSpec target)
    {
        if (!CanWeaponTarget(weapon, target))
        {
            return 0;
        }

        return PriorityFor(weapon.TargetProfile.WeightPriority, target.Stats.WeightClass)
            * PriorityFor(weapon.TargetProfile.DomainPriority, target.Movement.Domain)
            * PriorityFor(weapon.TargetProfile.ArmorPriority, target.Stats.ArmorTag);
    }

    private static float EffectiveDamageAgainst(AmmoDefinition ammo, UnitSpec target)
    {
        return ammo.BaseDamage * ammo.DamageProfile.Multiplier(
            target.Stats.WeightClass,
            target.Movement.Domain,
            target.Stats.ArmorTag);
    }

    private static float EffectiveDamageAgainst(AmmoDefinition ammo, BuildSpec targetSpec)
    {
        return ammo.BaseDamage * ammo.DamageProfile.Multiplier(
            UnitWeightClass.Heavy,
            MovementDomain.Land,
            targetSpec.ArmorTag);
    }

    private static float PriorityFor<T>(IReadOnlyDictionary<T, float> values, T key)
        where T : notnull
    {
        return values.TryGetValue(key, out var value) ? value : 1;
    }

    private static void ClearAttackTarget(UnitInstance unit)
    {
        unit.AttackTargetId = null;
        unit.AttackTargetKind = CombatTargetKind.Unit;
        unit.AttackTargetIsManual = false;
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

    private static void AimWeaponMounts(UnitInstance unit, float targetAngle, float dt)
    {
        for (var index = 0; index < unit.WeaponMounts.Count; index++)
        {
            var runtime = unit.WeaponMounts[index];
            var spec = unit.Spec.Weapons[index];
            var facing = spec.FacingMode == WeaponMountFacingMode.Independent
                ? RotateToward(runtime.Facing, targetAngle, MathF.Max(spec.TurnRate, unit.Spec.Movement.TurnRate) * dt)
                : unit.Facing;
            unit.WeaponMounts[index] = runtime with { Facing = facing };
        }
    }

    private static bool WeaponCanFireAt(float weaponFacing, float targetAngle, WeaponDefinition weapon)
    {
        var delta = MathF.Abs(Mathf.AngleDifference(weaponFacing, targetAngle));
        return delta <= MathF.Max(weapon.FireArcRadians, 0.08f) * 0.5f;
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
        var nearestEconomy = economyUnits.Min(unit => unit.Position.DistanceTo(center));
        var nearestNonEconomy = nonEconomyUnits.Min(unit => unit.Position.DistanceTo(center));
        return nearestEconomy <= nearestNonEconomy + SelectionEconomyIntentCenterMargin;
    }

    private static float RotateToward(float current, float target, float maxDelta)
    {
        var delta = Mathf.AngleDifference(current, target);
        return current + Mathf.Clamp(delta, -maxDelta, maxDelta);
    }
}
