using Godot;

namespace ProceduralRts.Core;

public sealed partial class GameState
{
    private void AcquireAutoTarget(UnitModel unit)
    {
        if (IsHarvesterUnit(unit) && unit.HarvesterMode != HarvesterMode.Idle)
        {
            return;
        }

        if (unit.Stance == UnitStance.Ignore)
        {
            unit.RetaliationTargetId = null;
            return;
        }

        if (unit.AttackTargetId is not null || (unit.MoveTarget is not null && unit.MoveMode != MoveCommandMode.Attack))
        {
            return;
        }

        var descriptor = unit.RuntimeDescriptor;
        UnitModel? target = unit.MoveMode == MoveCommandMode.Attack
            ? FindBestEnemyForWeapon(unit, descriptor.SightRange)
            : unit.Stance switch
            {
                UnitStance.Hold => FindBestEnemyForWeapon(unit, descriptor.AttackRange),
                UnitStance.Aggressive => FindBestEnemyForWeapon(unit, descriptor.SightRange),
                UnitStance.ReturnGuard => FindBestEnemyForWeapon(unit, descriptor.SightRange),
                UnitStance.PassiveRetaliate => unit.RetaliationTargetId is null ? null : UnitById(unit.RetaliationTargetId.Value),
                UnitStance.Ignore => null,
                _ => null,
            };

        if (target is null || !IsTargetableHostile(unit.Owner, target) || !CanUnitTarget(unit, CombatTargetKind.Unit, target.Id))
        {
            unit.RetaliationTargetId = null;
            ReturnToAnchorIfNeeded(unit);
            return;
        }

        var canEngage = unit.Stance switch
        {
            UnitStance.PassiveRetaliate => target.Position.DistanceTo(unit.Position) <= descriptor.AttackRange,
            UnitStance.Hold => target.Position.DistanceTo(unit.Position) <= descriptor.AttackRange,
            _ => target.Position.DistanceTo(unit.Position) <= descriptor.SightRange,
        };
        if (unit.MoveMode == MoveCommandMode.Attack)
        {
            canEngage = target.Position.DistanceTo(unit.Position) <= descriptor.SightRange;
        }

        if (!canEngage)
        {
            unit.RetaliationTargetId = null;
            ReturnToAnchorIfNeeded(unit);
            return;
        }

        AssignAttackTarget(
            unit,
            CombatTargetKind.Unit,
            target.Id,
            allowsPursuit: unit.Stance is UnitStance.Aggressive or UnitStance.ReturnGuard || unit.MoveMode == MoveCommandMode.Attack,
            returnToAnchor: unit.Stance == UnitStance.ReturnGuard,
            alert: false);
        ShareThreat(unit, CombatTargetKind.Unit, target.Id, target.Position);
    }

    private void AcquireBuildingAutoTarget(BuildingModel building)
    {
        var weapon = Weapon(building);
        if (weapon is null || building.Hp <= 0 || !building.Powered || building.BuildProgress < 1)
        {
            ClearBuildingAttackTarget(building);
            return;
        }

        if (building.AttackTargetId is not null)
        {
            var targetPosition = CombatTargetPosition(building.AttackTargetKind, building.AttackTargetId.Value);
            var targetIsHostile = IsCombatTargetHostile(building.Owner, building.AttackTargetKind, building.AttackTargetId.Value);
            if (targetPosition is not null
                && targetIsHostile
                && targetPosition.Value.DistanceTo(building.Position) <= weapon.Range
                && CanWeaponTarget(weapon, building.AttackTargetKind, building.AttackTargetId.Value))
            {
                return;
            }

            ClearBuildingAttackTarget(building);
        }

        var target = BestUnitTargetForWeapon(building.Owner, weapon, building.Position, weapon.Range, requirePositiveHp: true);
        if (target is null)
        {
            return;
        }

        building.AttackTargetId = target.Id;
        building.AttackTargetKind = CombatTargetKind.Unit;
    }

    private UnitModel? FindBestEnemyForWeapon(UnitModel unit, float range)
    {
        var weapon = Weapon(unit);
        return BestUnitTargetForWeapon(unit.Owner, weapon, unit.Position, range, requirePositiveHp: false);
    }

    private bool CanUnitTarget(UnitModel unit, CombatTargetKind targetKind, int targetId)
    {
        return CanWeaponTarget(Weapon(unit), targetKind, targetId);
    }

    private bool CanWeaponTarget(WeaponDefinition weapon, CombatTargetKind targetKind, int targetId)
    {
        return targetKind switch
        {
            CombatTargetKind.Unit when UnitById(targetId) is { } unit => WeaponCanTarget(weapon, unit.RuntimeDescriptor),
            CombatTargetKind.Building when BuildingById(targetId) is { } building => WeaponCanTarget(weapon, BuildSpecCatalog.For(building.Kind)),
            _ => false,
        };
    }

    private float TargetScore(WeaponDefinition weapon, Vector2 sourcePosition, CombatTargetKind targetKind, int targetId, float searchRange)
    {
        var priority = targetKind switch
        {
            CombatTargetKind.Unit when UnitById(targetId) is { } unit => WeaponTargetPriority(weapon, unit.RuntimeDescriptor),
            CombatTargetKind.Building when BuildingById(targetId) is { } building => WeaponTargetPriority(weapon, BuildSpecCatalog.For(building.Kind)),
            _ => 0,
        };
        if (priority <= 0)
        {
            return 0;
        }

        var targetPosition = CombatTargetPosition(targetKind, targetId);
        if (targetPosition is null)
        {
            return 0;
        }

        var distance = sourcePosition.DistanceTo(targetPosition.Value);
        var distanceScore = 1 - Mathf.Clamp(distance / MathF.Max(1, searchRange), 0, 1);
        return priority * 1000 + distanceScore * 120;
    }

    private void ReturnToAnchorIfNeeded(UnitModel unit)
    {
        if (unit.Stance != UnitStance.ReturnGuard || unit.Position.DistanceTo(unit.AnchorPosition) <= 8)
        {
            return;
        }

        AssignPath(unit, unit.AnchorPosition, unit.AnchorPosition);
    }

    private static void ClearBuildingAttackTarget(BuildingModel building)
    {
        building.AttackTargetId = null;
        building.AttackTargetKind = CombatTargetKind.Unit;
        building.TurretState = building.AttackCooldownRemaining > 0 ? TurretState.Reloading : TurretState.Idle;
    }

    private void ClearAttackTarget(UnitModel unit)
    {
        var shouldReturn = unit.ReturnToAnchorAfterAttack;
        unit.AttackTargetId = null;
        unit.AttackTargetKind = CombatTargetKind.Unit;
        unit.AttackTargetIsManual = false;
        unit.AttackTargetAllowsPursuit = false;
        ClearAttackTrackingMemory(unit);
        unit.ReturnToAnchorAfterAttack = false;
        unit.RetaliationTargetId = null;
        if (unit.MovementState == UnitMovementState.CombatAnchor)
        {
            unit.MoveTarget = null;
            unit.Path.Clear();
            unit.GlobalCorridor.Clear();
            unit.Velocity = Vector2.Zero;
            unit.FormationSlot = null;
            unit.MovementState = UnitMovementState.Idle;
        }

        if (shouldReturn)
        {
            ReturnToAnchorIfNeeded(unit);
        }
    }

    private void NotifyAttacked(UnitModel target, int sourceId)
    {
        if (target.Stance == UnitStance.Ignore)
        {
            target.AlertPulse = 0.45f;
            return;
        }

        target.RetaliationTargetId = sourceId;
        target.AlertPulse = 1;

        if (target.Stance == UnitStance.PassiveRetaliate && target.AttackTargetId is null)
        {
            var attacker = UnitById(sourceId);
            if (attacker is not null && target.Position.DistanceTo(attacker.Position) <= target.RuntimeDescriptor.AttackRange)
            {
                AssignAttackTarget(target, CombatTargetKind.Unit, attacker.Id, allowsPursuit: false, returnToAnchor: false);
            }
        }

        if (UnitById(sourceId) is { } source)
        {
            ShareThreat(target, CombatTargetKind.Unit, source.Id, source.Position, force: true);
        }
    }

    private void AssignAttackTarget(
        UnitModel unit,
        CombatTargetKind targetKind,
        int targetId,
        bool allowsPursuit,
        bool returnToAnchor,
        bool alert = true)
    {
        if (!CanUnitTarget(unit, targetKind, targetId))
        {
            return;
        }

        unit.AttackTargetId = targetId;
        unit.AttackTargetKind = targetKind;
        unit.AttackTargetIsManual = false;
        unit.AttackTargetAllowsPursuit = allowsPursuit;
        if (CombatTargetPosition(targetKind, targetId) is { } targetPosition)
        {
            RememberAttackTargetPosition(unit, targetPosition);
        }

        unit.ReturnToAnchorAfterAttack = returnToAnchor;
        ClearMoveTarget(unit);

        if (alert)
        {
            unit.AlertPulse = 1;
            unit.CommandPulse = Mathf.Max(unit.CommandPulse, 0.55f);
        }
    }

    private void ShareThreat(
        UnitModel source,
        CombatTargetKind targetKind,
        int targetId,
        Vector2 threatPosition,
        bool force = false)
    {
        if (!force && source.ThreatShareCooldownRemaining > 0)
        {
            return;
        }

        source.ThreatShareCooldownRemaining = SharedThreatMemorySeconds;
        source.AlertPulse = 1;
        ShareThreat(source.Owner, source.Position, source.Id, targetKind, targetId, threatPosition);
    }

    private void ShareThreat(
        Owner owner,
        Vector2 originPosition,
        int? sourceUnitId,
        CombatTargetKind targetKind,
        int targetId,
        Vector2 threatPosition)
    {
        if (!IsCombatTargetHostile(owner, targetKind, targetId))
        {
            return;
        }

        var threatKey = ThreatKey(targetKind, targetId);
        foreach (var ally in Units)
        {
            if (!IsOwnerAllied(owner, ally.Owner) || ally.Id == sourceUnitId)
            {
                continue;
            }

            if (ally.Stance == UnitStance.Ignore)
            {
                continue;
            }

            if (ally.AttackTargetIsManual || ally.AttackTargetId is not null || ally.MoveTarget is not null)
            {
                continue;
            }

            if (ally.LastSharedThreatKey == threatKey && ally.ThreatShareCooldownRemaining > 0)
            {
                continue;
            }

            if (!TryCreateSharedThreatResponse(ally, originPosition, threatPosition, out var allowsPursuit, out var returnToAnchor))
            {
                continue;
            }

            ally.LastSharedThreatKey = threatKey;
            ally.ThreatShareCooldownRemaining = SharedThreatMemorySeconds;
            AssignAttackTarget(ally, targetKind, targetId, allowsPursuit, returnToAnchor);
        }
    }

    private bool TryCreateSharedThreatResponse(
        UnitModel ally,
        Vector2 originPosition,
        Vector2 threatPosition,
        out bool allowsPursuit,
        out bool returnToAnchor)
    {
        var descriptor = ally.RuntimeDescriptor;
        var distanceToOrigin = ally.Position.DistanceTo(originPosition);
        var distanceToThreat = ally.Position.DistanceTo(threatPosition);
        allowsPursuit = false;
        returnToAnchor = false;

        return ally.Stance switch
        {
            UnitStance.Hold => distanceToOrigin <= AllyThreatShareRadius
                && distanceToThreat <= descriptor.AttackRange + HoldThreatLinkSlack,
            UnitStance.Aggressive => SetResponse(distanceToOrigin <= AllyThreatShareRadius
                && distanceToThreat <= descriptor.SightRange, true, false, out allowsPursuit, out returnToAnchor),
            UnitStance.ReturnGuard => SetResponse(distanceToOrigin <= AllyThreatShareRadius
                && threatPosition.DistanceTo(ally.AnchorPosition) <= descriptor.SightRange * 1.25f, true, true, out allowsPursuit, out returnToAnchor),
            UnitStance.PassiveRetaliate => distanceToOrigin <= PassiveAllyCallRadius
                && distanceToThreat <= descriptor.AttackRange + HoldThreatLinkSlack * 0.5f,
            UnitStance.Ignore => false,
            _ => false,
        };
    }

    private static bool SetResponse(
        bool canRespond,
        bool pursuit,
        bool returnAfter,
        out bool allowsPursuit,
        out bool returnToAnchor)
    {
        allowsPursuit = canRespond && pursuit;
        returnToAnchor = canRespond && returnAfter;
        return canRespond;
    }

    private float StationaryThreatLeash(UnitModel unit)
    {
        return unit.RuntimeDescriptor.AttackRange + unit.Stance switch
        {
            UnitStance.PassiveRetaliate => HoldThreatLinkSlack * 0.5f,
            UnitStance.Hold => HoldThreatLinkSlack,
            UnitStance.Ignore => 0,
            _ => HoldThreatLinkSlack,
        };
    }

    private static int ThreatKey(CombatTargetKind targetKind, int targetId)
    {
        return ((int)targetKind * 100_000) + targetId;
    }
}
