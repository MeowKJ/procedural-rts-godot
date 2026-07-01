using Godot;

namespace ProceduralRts.Core;

public sealed class UnitBattlefieldEnemyAttackWaveAi
{
    private const float DefenseCheckInterval = 1.0f;
    private const float DefenseRadius = 900f;

    private readonly EnemyDifficultyProfile _profile;
    private float _waveTimer;
    private float _defenseTimer;

    public int WavesLaunched { get; private set; }
    public int DefenseOrders { get; private set; }
    public string LastStatus { get; private set; } = "Enemy attack waves forming";

    public UnitBattlefieldEnemyAttackWaveAi()
        : this(EnemyDifficultyProfile.Normal)
    {
    }

    public UnitBattlefieldEnemyAttackWaveAi(EnemyDifficultyProfile profile)
    {
        _profile = profile;
        _waveTimer = profile.AttackInitialDelay;
    }

    public void Update(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId, double delta)
    {
        _defenseTimer -= (float)delta;
        if (_defenseTimer <= 0)
        {
            _defenseTimer = DefenseCheckInterval;
            if (TryIssueDefenseOrder(battlefield, enemyPlayerSlotId, out var defenseStatus))
            {
                DefenseOrders++;
                LastStatus = defenseStatus;
                return;
            }
        }

        _waveTimer -= (float)delta;
        if (_waveTimer > 0)
        {
            return;
        }

        battlefield.RebuildVisibilityIndex();
        var waveUnits = AvailableWaveUnits(
            battlefield,
            enemyPlayerSlotId,
            _profile.MinimumWaveUnits,
            _profile.MaximumWaveUnits).ToList();
        if (waveUnits.Count < _profile.MinimumWaveUnits)
        {
            _waveTimer = 5f;
            LastStatus = $"Enemy wave waiting ({waveUnits.Count}/{_profile.MinimumWaveUnits})";
            return;
        }

        if (!TryFindTarget(battlefield, enemyPlayerSlotId, _profile.AggressionRadius, out var targetKind, out var targetUnit, out var targetBuilding, out var targetPosition))
        {
            if (TryIssueScoutWave(battlefield, enemyPlayerSlotId, waveUnits, out var scoutStatus))
            {
                WavesLaunched++;
                _waveTimer = Math.Min(_profile.AttackWaveInterval, 6f);
                LastStatus = scoutStatus;
                return;
            }

            _waveTimer = 8f;
            LastStatus = "Enemy wave has no visible target";
            return;
        }

        var unitIds = waveUnits.Select(unit => unit.Id).ToList();
        var commanded = targetKind == CombatTargetKind.Building && targetBuilding is { } buildingTarget
            ? battlefield.CommandAttackUnits(enemyPlayerSlotId, unitIds, buildingTarget.Id)
            : targetUnit is not null
                ? battlefield.CommandAttackUnits(enemyPlayerSlotId, unitIds, targetUnit)
                : 0;
        if (commanded == 0)
        {
            _waveTimer = 4f;
            LastStatus = "Enemy wave has no valid weapon target";
            return;
        }

        foreach (var unit in waveUnits.Where(unit => unit.AttackTargetId is not null))
        {
            unit.PlayerIntentTarget = targetPosition;
            unit.CommandVisualTarget = targetPosition;
            unit.CommandPulse = 1;
        }

        WavesLaunched++;
        _waveTimer = _profile.AttackWaveInterval;
        LastStatus = $"Enemy wave launched ({commanded} units)";
    }

    private static bool TryIssueDefenseOrder(UnitBattlefield battlefield, PlayerSlotId playerSlotId, out string status)
    {
        status = string.Empty;
        if (battlefield.LiveBuildingCount(playerSlotId) == 0)
        {
            return false;
        }

        battlefield.RebuildVisibilityIndex();
        if (!TryFindDefenseTarget(battlefield, playerSlotId, out var targetKind, out var targetUnit, out var targetBuilding, out var targetPosition))
        {
            return false;
        }

        var defenders = AvailableDefenseUnits(battlefield, playerSlotId, targetPosition)
            .Take(6)
            .ToList();
        if (defenders.Count == 0)
        {
            return false;
        }

        var defenderIds = defenders.Select(unit => unit.Id).ToList();
        var commanded = targetKind == CombatTargetKind.Building && targetBuilding is { } buildingTarget
            ? battlefield.CommandAttackUnits(playerSlotId, defenderIds, buildingTarget.Id)
            : targetUnit is not null
                ? battlefield.CommandAttackUnits(playerSlotId, defenderIds, targetUnit)
                : 0;
        if (commanded == 0)
        {
            return false;
        }

        foreach (var defender in defenders)
        {
            defender.PlayerIntentTarget = targetPosition;
            defender.CommandVisualTarget = targetPosition;
            defender.CommandPulse = 1;
        }

        status = $"Enemy defense ordered ({commanded} units)";
        return true;
    }

    private static IEnumerable<UnitInstance> AvailableDefenseUnits(UnitBattlefield battlefield, PlayerSlotId playerSlotId, Vector2 targetPosition)
    {
        var baseCenter = EnemyBaseCenter(battlefield, playerSlotId);
        return AvailableCombatUnits(battlefield, playerSlotId)
            .Where(unit => unit.Position.DistanceSquaredTo(baseCenter) <= DefenseRadius * DefenseRadius
                || unit.Position.DistanceSquaredTo(targetPosition) <= DefenseRadius * DefenseRadius)
            .OrderBy(unit => unit.Position.DistanceSquaredTo(targetPosition))
            .ThenBy(unit => unit.Id);
    }

    private static IEnumerable<UnitInstance> AvailableWaveUnits(
        UnitBattlefield battlefield,
        PlayerSlotId playerSlotId,
        int minimumWaveUnits,
        int maximumWaveUnits)
    {
        var baseCenter = EnemyBaseCenter(battlefield, playerSlotId);
        var candidates = AvailableCombatUnits(battlefield, playerSlotId)
            .OrderBy(unit => unit.Position.DistanceSquaredTo(baseCenter))
            .ThenBy(unit => unit.Id)
            .ToList();
        var reserveCount = Math.Min(3, Math.Max(0, candidates.Count - minimumWaveUnits));
        var reserved = candidates
            .Take(reserveCount)
            .Select(unit => unit.Id)
            .ToHashSet();

        return candidates
            .Where(unit => !reserved.Contains(unit.Id))
            .OrderBy(unit => unit.Position.X)
            .ThenBy(unit => unit.Id)
            .Take(maximumWaveUnits);
    }

    private static IEnumerable<UnitInstance> AvailableCombatUnits(UnitBattlefield battlefield, PlayerSlotId playerSlotId)
    {
        return battlefield.Units
            .Where(unit => unit.PlayerSlotId == playerSlotId)
            .Where(unit => unit.Hp > 0)
            .Where(unit => !unit.Spec.RoleTags.Contains(UnitRoleTag.Economy))
            .Where(unit => unit.AttackTargetId is null || !unit.AttackTargetIsManual);
    }

    private bool TryFindTarget(
        UnitBattlefield battlefield,
        PlayerSlotId enemyPlayerSlotId,
        float aggressionRadius,
        out CombatTargetKind targetKind,
        out UnitInstance? targetUnit,
        out UnitBattlefieldBuildingSnapshot? targetBuilding,
        out Vector2 targetPosition)
    {
        var enemyCenter = EnemyCenter(battlefield, enemyPlayerSlotId);
        var buildings = battlefield.BuildingSnapshots();
        targetBuilding = buildings
            .Where(building => battlefield.Relations.CanAttack(enemyPlayerSlotId, building.PlayerSlotId))
            .Where(building => building.Hp > 0)
            .Where(building => battlefield.IsVisibleTo(enemyPlayerSlotId, building.Id))
            .Where(building => building.Kind == BuildingDesignIds.Headquarters
                && IsInsideAggressionRadius(building.Position, enemyCenter, aggressionRadius))
            .Select(building => (UnitBattlefieldBuildingSnapshot?)building)
            .FirstOrDefault();
        if (targetBuilding is { } headquarters)
        {
            targetKind = CombatTargetKind.Building;
            targetUnit = null;
            targetPosition = headquarters.Position;
            return true;
        }

        targetBuilding = buildings
            .Where(building => battlefield.Relations.CanAttack(enemyPlayerSlotId, building.PlayerSlotId) && building.Hp > 0)
            .Where(building => battlefield.IsVisibleTo(enemyPlayerSlotId, building.Id))
            .Where(building => IsInsideAggressionRadius(building.Position, enemyCenter, aggressionRadius))
            .OrderBy(building => building.Position.DistanceSquaredTo(enemyCenter))
            .Select(building => (UnitBattlefieldBuildingSnapshot?)building)
            .FirstOrDefault();
        if (targetBuilding is { } nearestBuilding)
        {
            targetKind = CombatTargetKind.Building;
            targetUnit = null;
            targetPosition = nearestBuilding.Position;
            return true;
        }

        targetUnit = battlefield.Units
            .Where(unit => battlefield.Relations.CanAttack(enemyPlayerSlotId, unit.PlayerSlotId) && unit.Hp > 0)
            .Where(unit => battlefield.IsVisibleTo(enemyPlayerSlotId, unit))
            .Where(unit => IsInsideAggressionRadius(unit.Position, enemyCenter, aggressionRadius))
            .OrderBy(unit => unit.Position.DistanceSquaredTo(enemyCenter))
            .FirstOrDefault();
        if (targetUnit is not null)
        {
            targetKind = CombatTargetKind.Unit;
            targetBuilding = null;
            targetPosition = targetUnit.Position;
            return true;
        }

        targetKind = CombatTargetKind.Unit;
        targetPosition = Vector2.Zero;
        return false;
    }

    private static bool TryIssueScoutWave(
        UnitBattlefield battlefield,
        PlayerSlotId enemyPlayerSlotId,
        IReadOnlyList<UnitInstance> waveUnits,
        out string status)
    {
        status = string.Empty;
        var scoutPoint = ScoutPoint(battlefield, enemyPlayerSlotId);
        var moved = battlefield.CommandMoveUnits(
            enemyPlayerSlotId,
            waveUnits.Select(unit => unit.Id),
            scoutPoint,
            battlefield.WorldSize,
            MoveCommandMode.Attack);
        if (moved == 0)
        {
            return false;
        }

        foreach (var unit in waveUnits)
        {
            unit.PlayerIntentTarget = scoutPoint;
            unit.CommandVisualTarget = scoutPoint;
            unit.CommandPulse = 1;
        }

        status = $"Enemy scout wave launched ({moved} units)";
        return true;
    }

    private static bool TryFindDefenseTarget(
        UnitBattlefield battlefield,
        PlayerSlotId playerSlotId,
        out CombatTargetKind targetKind,
        out UnitInstance? targetUnit,
        out UnitBattlefieldBuildingSnapshot? targetBuilding,
        out Vector2 targetPosition)
    {
        var baseCenter = EnemyBaseCenter(battlefield, playerSlotId);
        targetUnit = battlefield.Units
            .Where(unit => battlefield.Relations.CanAttack(playerSlotId, unit.PlayerSlotId) && unit.Hp > 0)
            .Where(unit => battlefield.IsVisibleTo(playerSlotId, unit))
            .Where(unit => unit.Position.DistanceSquaredTo(baseCenter) <= DefenseRadius * DefenseRadius
                || IsNearOwnedBuilding(battlefield, playerSlotId, unit.Position, DefenseRadius))
            .OrderBy(unit => unit.Position.DistanceSquaredTo(baseCenter))
            .ThenBy(unit => unit.Id)
            .FirstOrDefault();
        if (targetUnit is not null)
        {
            targetKind = CombatTargetKind.Unit;
            targetBuilding = null;
            targetPosition = targetUnit.Position;
            return true;
        }

        targetBuilding = battlefield.BuildingSnapshots()
            .Where(building => battlefield.Relations.CanAttack(playerSlotId, building.PlayerSlotId) && building.Hp > 0)
            .Where(building => battlefield.IsVisibleTo(playerSlotId, building.Id))
            .Where(building => building.Position.DistanceSquaredTo(baseCenter) <= DefenseRadius * DefenseRadius)
            .OrderBy(building => building.Position.DistanceSquaredTo(baseCenter))
            .ThenBy(building => building.Id)
            .Select(building => (UnitBattlefieldBuildingSnapshot?)building)
            .FirstOrDefault();
        if (targetBuilding is { } defendedBuilding)
        {
            targetKind = CombatTargetKind.Building;
            targetUnit = null;
            targetPosition = defendedBuilding.Position;
            return true;
        }

        targetKind = CombatTargetKind.Unit;
        targetPosition = Vector2.Zero;
        return false;
    }

    private static bool IsNearOwnedBuilding(UnitBattlefield battlefield, PlayerSlotId playerSlotId, Vector2 position, float radius)
    {
        return battlefield.BuildingSnapshots()
            .Where(building => building.PlayerSlotId == playerSlotId && building.Hp > 0)
            .Any(building => building.Position.DistanceSquaredTo(position) <= radius * radius);
    }

    private static Vector2 ScoutPoint(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        var center = EnemyBaseCenter(battlefield, enemyPlayerSlotId);
        return new Vector2(
            Mathf.Clamp(battlefield.WorldSize.X - center.X, 180, battlefield.WorldSize.X - 180),
            Mathf.Clamp(battlefield.WorldSize.Y - center.Y, 180, battlefield.WorldSize.Y - 180));
    }

    private static bool IsInsideAggressionRadius(Vector2 targetPosition, Vector2 enemyCenter, float aggressionRadius)
    {
        if (float.IsPositiveInfinity(aggressionRadius))
        {
            return true;
        }

        return targetPosition.DistanceSquaredTo(enemyCenter) <= aggressionRadius * aggressionRadius;
    }

    private static Vector2 EnemyBaseCenter(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        var buildings = battlefield.BuildingSnapshots()
            .Where(building => building.PlayerSlotId == enemyPlayerSlotId && building.Hp > 0)
            .Select(building => building.Position)
            .ToList();
        if (buildings.Count > 0)
        {
            return buildings.Aggregate(Vector2.Zero, (sum, position) => sum + position) / buildings.Count;
        }

        return EnemyCenter(battlefield, enemyPlayerSlotId);
    }

    private static Vector2 EnemyCenter(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        var units = battlefield.Units
            .Where(unit => unit.PlayerSlotId == enemyPlayerSlotId && unit.Hp > 0)
            .Select(unit => unit.Position)
            .ToList();
        if (units.Count == 0)
        {
            return new Vector2(battlefield.WorldSize.X * 0.78f, battlefield.WorldSize.Y * 0.62f);
        }

        return units.Aggregate(Vector2.Zero, (sum, position) => sum + position) / units.Count;
    }
}
