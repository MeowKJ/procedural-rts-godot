using Godot;

namespace ProceduralRts.Core;

public sealed partial class GameState
{
    private IReadOnlyList<SpawnObstacle> UnitObstacles()
    {
        return Units
            .Select(unit => new SpawnObstacle(unit.Position.X, unit.Position.Y, unit.RuntimeDescriptor.Radius))
            .ToList();
    }

    private void RemoveDeadUnits()
    {
        var removedUnits = Units.Where(unit => unit.Hp <= 0).ToList();
        var removedIds = removedUnits.Select(unit => unit.Id).ToList();
        if (removedIds.Count == 0)
        {
            return;
        }

        var deaths = removedUnits
            .Select(unit =>
            {
                var descriptor = unit.RuntimeDescriptor;
                return new UnitDeathInfo(
                    unit.Id,
                    unit.DesignId,
                    unit.Owner,
                    unit.FactionId,
                    unit.Position,
                    descriptor.Radius,
                    descriptor.WeightClass,
                    descriptor.MovementDomain,
                    unit.LastDamageAmmoKind,
                    unit.DeathOverkillDamage);
            })
            .ToList();

        Units.RemoveAll(unit => removedIds.Contains(unit.Id));
        Projectiles.RemoveAll(projectile => (projectile.SourceKind == CombatSourceKind.Unit && removedIds.Contains(projectile.SourceId)) || (projectile.TargetKind == CombatTargetKind.Unit && removedIds.Contains(projectile.TargetId)));
        Beams.RemoveAll(beam => (beam.SourceKind == CombatSourceKind.Unit && removedIds.Contains(beam.SourceId)) || (beam.TargetKind == CombatTargetKind.Unit && removedIds.Contains(beam.TargetId)));

        foreach (var unit in Units)
        {
            if (unit.AttackTargetId is not null && unit.AttackTargetKind == CombatTargetKind.Unit && removedIds.Contains(unit.AttackTargetId.Value))
            {
                ClearAttackTarget(unit);
                ClearMoveTarget(unit);
            }
        }

        foreach (var building in Buildings)
        {
            if (building.AttackTargetId is not null && building.AttackTargetKind == CombatTargetKind.Unit && removedIds.Contains(building.AttackTargetId.Value))
            {
                ClearBuildingAttackTarget(building);
            }
        }

        UnitsRemoved?.Invoke(deaths);
    }

    private void RemoveDeadBuildings()
    {
        var removedIds = Buildings.Where(building => building.Hp <= 0).Select(building => building.Id).ToList();
        if (removedIds.Count == 0)
        {
            return;
        }

        var removedBuildings = Buildings.Where(building => removedIds.Contains(building.Id)).ToList();
        Buildings.RemoveAll(building => removedIds.Contains(building.Id));
        Projectiles.RemoveAll(projectile => (projectile.SourceKind == CombatSourceKind.Building && removedIds.Contains(projectile.SourceId)) || (projectile.TargetKind == CombatTargetKind.Building && removedIds.Contains(projectile.TargetId)));
        Beams.RemoveAll(beam => (beam.SourceKind == CombatSourceKind.Building && removedIds.Contains(beam.SourceId)) || (beam.TargetKind == CombatTargetKind.Building && removedIds.Contains(beam.TargetId)));

        foreach (var unit in Units)
        {
            if (unit.AttackTargetId is not null && unit.AttackTargetKind == CombatTargetKind.Building && removedIds.Contains(unit.AttackTargetId.Value))
            {
                ClearAttackTarget(unit);
                ClearMoveTarget(unit);
            }
        }

        BuildingsRemoved?.Invoke(removedIds);
        UpdateOutcomeAfterRemovedBuildings(removedBuildings);
    }

    private void UpdateOutcomeAfterRemovedBuildings(IReadOnlyList<BuildingModel> removedBuildings)
    {
        if (Outcome != GameOutcome.InProgress)
        {
            return;
        }

        if (removedBuildings.Any(building => building.Kind == BuildingDesignIds.Headquarters && IsHostileToPlayer(building)))
        {
            Outcome = GameOutcome.Victory;
            OutcomeChanged?.Invoke(Outcome);
            return;
        }

        if (removedBuildings.Any(building => building.Kind == BuildingDesignIds.Headquarters && IsAlliedWithPlayer(building)))
        {
            Outcome = GameOutcome.Defeat;
            OutcomeChanged?.Invoke(Outcome);
        }
    }

    private Owner? CombatTargetOwner(CombatTargetKind targetKind, int targetId)
    {
        return targetKind switch
        {
            CombatTargetKind.Unit => UnitById(targetId)?.Owner,
            CombatTargetKind.Building => BuildingById(targetId)?.Owner,
            _ => null,
        };
    }

    private bool IsCombatTargetHostile(Owner viewerOwner, CombatTargetKind targetKind, int targetId)
    {
        var targetOwner = CombatTargetOwner(targetKind, targetId);
        return targetOwner is not null && CanOwnerAttack(viewerOwner, targetOwner.Value);
    }

    private void ApplyDamage(CombatTargetKind targetKind, int targetId, float damage, CombatSourceKind sourceKind, int sourceId)
    {
        switch (targetKind)
        {
            case CombatTargetKind.Unit:
                var unit = UnitById(targetId);
                if (unit is null)
                {
                    return;
                }

                unit.Hp -= damage;
                unit.LastDamageAmount = damage;
                unit.LastDamageAmmoKind = SourceAmmoKind(sourceKind, sourceId);
                unit.DeathOverkillDamage = MathF.Max(0, -unit.Hp);
                unit.HitPulse = 1;
                EntityAttacked?.Invoke(unit.Owner, unit.FactionId, unit.Position, unit.RuntimeDescriptor.Label);
                if (sourceKind == CombatSourceKind.Unit)
                {
                    NotifyAttacked(unit, sourceId);
                }
                break;
            case CombatTargetKind.Building:
                var building = BuildingById(targetId);
                if (building is null)
                {
                    return;
                }

                building.Hp -= damage;
                building.HitPulse = 1;
                EntityAttacked?.Invoke(building.Owner, building.FactionId, building.Position, BuildSpecCatalog.For(building.Kind).Label);
                if (sourceKind == CombatSourceKind.Unit && UnitById(sourceId) is { } attacker)
                {
                    ShareThreat(building.Owner, building.Position, null, CombatTargetKind.Unit, attacker.Id, attacker.Position);
                }
                break;
        }
    }

    private AmmoKind? SourceAmmoKind(CombatSourceKind sourceKind, int sourceId)
    {
        return sourceKind switch
        {
            CombatSourceKind.Unit when UnitById(sourceId) is { } unit => Weapon(unit).AmmoKind,
            CombatSourceKind.Building when BuildingById(sourceId) is { } building => Weapon(building)?.AmmoKind,
            _ => null,
        };
    }

    private float BuildingRadius(BuildingModel building)
    {
        var spec = BuildSpecCatalog.For(building.Kind);
        return Mathf.Max(spec.Footprint.X, spec.Footprint.Y) * 0.5f;
    }

    private Vector2 ClampInsideWorld(Vector2 point, float margin)
    {
        return new Vector2(
            Mathf.Clamp(point.X, margin, WorldSize.X - margin),
            Mathf.Clamp(point.Y, margin, WorldSize.Y - margin));
    }

    private static bool IsProductionBuilding(BuildingModel building)
    {
        foreach (var spec in ProductionKindDesignBridge.PlayableProductionSpecs(ProductionKindDesignBridge.UnitFactionFor(building.FactionId)))
        {
            if (spec.Production?.ProducerKind == building.Kind)
            {
                return true;
            }
        }

        return false;
    }

    private static float RotateToward(float current, float target, float maxDelta)
    {
        var delta = Mathf.AngleDifference(current, target);
        return current + Mathf.Clamp(delta, -maxDelta, maxDelta);
    }
}
