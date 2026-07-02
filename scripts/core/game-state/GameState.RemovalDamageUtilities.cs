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
        _legacyUnitDeathBuffer.Clear();
        _legacyRemovedUnitIds.Clear();
        foreach (var unit in Units)
        {
            if (unit.Hp > 0)
            {
                continue;
            }

            var descriptor = unit.RuntimeDescriptor;
            _legacyUnitDeathBuffer.Add(new UnitDeathInfo(
                unit.Id,
                unit.DesignId,
                unit.Owner,
                unit.FactionId,
                unit.Position,
                descriptor.Radius,
                descriptor.WeightClass,
                descriptor.MovementDomain,
                unit.LastDamageAmmoKind,
                unit.DeathOverkillDamage));
            _legacyRemovedUnitIds.Add(unit.Id);
        }

        if (_legacyUnitDeathBuffer.Count == 0)
        {
            return;
        }

        Units.RemoveAll(IsLegacyRemovedUnit);
        Projectiles.RemoveAll(IsLegacyProjectileLinkedToRemovedUnit);
        Beams.RemoveAll(IsLegacyBeamLinkedToRemovedUnit);

        foreach (var unit in Units)
        {
            if (unit.AttackTargetId is not null
                && unit.AttackTargetKind == CombatTargetKind.Unit
                && _legacyRemovedUnitIds.Contains(unit.AttackTargetId.Value))
            {
                ClearAttackTarget(unit);
                ClearMoveTarget(unit);
            }
        }

        foreach (var building in Buildings)
        {
            if (building.AttackTargetId is not null
                && building.AttackTargetKind == CombatTargetKind.Unit
                && _legacyRemovedUnitIds.Contains(building.AttackTargetId.Value))
            {
                ClearBuildingAttackTarget(building);
            }
        }

        UnitsRemoved?.Invoke(_legacyUnitDeathBuffer);
    }

    private void RemoveDeadBuildings()
    {
        _legacyRemovedBuildingIds.Clear();
        _legacyRemovedBuildingIdSet.Clear();
        _legacyRemovedBuildings.Clear();
        foreach (var building in Buildings)
        {
            if (building.Hp > 0)
            {
                continue;
            }

            _legacyRemovedBuildingIds.Add(building.Id);
            _legacyRemovedBuildingIdSet.Add(building.Id);
            _legacyRemovedBuildings.Add(building);
        }

        if (_legacyRemovedBuildingIds.Count == 0)
        {
            return;
        }

        Buildings.RemoveAll(IsLegacyRemovedBuilding);
        Projectiles.RemoveAll(IsLegacyProjectileLinkedToRemovedBuilding);
        Beams.RemoveAll(IsLegacyBeamLinkedToRemovedBuilding);

        foreach (var unit in Units)
        {
            if (unit.AttackTargetId is not null
                && unit.AttackTargetKind == CombatTargetKind.Building
                && _legacyRemovedBuildingIdSet.Contains(unit.AttackTargetId.Value))
            {
                ClearAttackTarget(unit);
                ClearMoveTarget(unit);
            }
        }

        BuildingsRemoved?.Invoke(_legacyRemovedBuildingIds);
        UpdateOutcomeAfterRemovedBuildings(_legacyRemovedBuildings);
    }

    private void UpdateOutcomeAfterRemovedBuildings(IReadOnlyList<BuildingModel> removedBuildings)
    {
        if (Outcome != GameOutcome.InProgress)
        {
            return;
        }

        foreach (var building in removedBuildings)
        {
            if (building.Kind == BuildingDesignIds.Headquarters && IsHostileToPlayer(building))
            {
                Outcome = GameOutcome.Victory;
                OutcomeChanged?.Invoke(Outcome);
                return;
            }
        }

        foreach (var building in removedBuildings)
        {
            if (building.Kind == BuildingDesignIds.Headquarters && IsAlliedWithPlayer(building))
            {
                Outcome = GameOutcome.Defeat;
                OutcomeChanged?.Invoke(Outcome);
                return;
            }
        }
    }

    private bool IsLegacyRemovedUnit(UnitModel unit)
    {
        return _legacyRemovedUnitIds.Contains(unit.Id);
    }

    private bool IsLegacyProjectileLinkedToRemovedUnit(ProjectileModel projectile)
    {
        return (projectile.SourceKind == CombatSourceKind.Unit && _legacyRemovedUnitIds.Contains(projectile.SourceId))
            || (projectile.TargetKind == CombatTargetKind.Unit && _legacyRemovedUnitIds.Contains(projectile.TargetId));
    }

    private bool IsLegacyBeamLinkedToRemovedUnit(BeamModel beam)
    {
        return (beam.SourceKind == CombatSourceKind.Unit && _legacyRemovedUnitIds.Contains(beam.SourceId))
            || (beam.TargetKind == CombatTargetKind.Unit && _legacyRemovedUnitIds.Contains(beam.TargetId));
    }

    private bool IsLegacyRemovedBuilding(BuildingModel building)
    {
        return _legacyRemovedBuildingIdSet.Contains(building.Id);
    }

    private bool IsLegacyProjectileLinkedToRemovedBuilding(ProjectileModel projectile)
    {
        return (projectile.SourceKind == CombatSourceKind.Building && _legacyRemovedBuildingIdSet.Contains(projectile.SourceId))
            || (projectile.TargetKind == CombatTargetKind.Building && _legacyRemovedBuildingIdSet.Contains(projectile.TargetId));
    }

    private bool IsLegacyBeamLinkedToRemovedBuilding(BeamModel beam)
    {
        return (beam.SourceKind == CombatSourceKind.Building && _legacyRemovedBuildingIdSet.Contains(beam.SourceId))
            || (beam.TargetKind == CombatTargetKind.Building && _legacyRemovedBuildingIdSet.Contains(beam.TargetId));
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
