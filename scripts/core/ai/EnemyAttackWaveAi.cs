using Godot;

namespace ProceduralRts.Core;

public sealed class EnemyAttackWaveAi
{
    private readonly EnemyDifficultyProfile _profile;
    private readonly List<UnitModel> _waveUnits = [];
    private float _waveTimer;

    public int WavesLaunched { get; private set; }
    public string LastStatus { get; private set; } = "Enemy attack waves forming";

    public EnemyAttackWaveAi()
        : this(EnemyDifficultyProfile.Normal)
    {
    }

    public EnemyAttackWaveAi(EnemyDifficultyProfile profile)
    {
        _profile = profile;
        _waveTimer = profile.AttackInitialDelay;
    }

    public void Update(GameState state, double delta)
    {
        _waveTimer -= (float)delta;
        if (_waveTimer > 0)
        {
            return;
        }

        CollectAvailableCombatUnits(state, _profile.MaximumWaveUnits, _waveUnits);
        if (_waveUnits.Count < _profile.MinimumWaveUnits)
        {
            _waveTimer = 5f;
            LastStatus = $"Enemy wave waiting ({_waveUnits.Count}/{_profile.MinimumWaveUnits})";
            return;
        }

        if (!TryFindTarget(state, _profile.AggressionRadius, out var targetKind, out var targetId, out var targetPosition))
        {
            _waveTimer = 8f;
            LastStatus = "Enemy wave has no target";
            return;
        }

        foreach (var unit in _waveUnits)
        {
            unit.AttackTargetId = targetId;
            unit.AttackTargetKind = targetKind;
            unit.AttackTargetIsManual = true;
            unit.AttackTargetAllowsPursuit = true;
            unit.ReturnToAnchorAfterAttack = false;
            unit.LastSharedThreatKey = null;
            unit.ThreatShareCooldownRemaining = 0.9f;
            unit.MoveTarget = null;
            unit.Path.Clear();
            unit.GlobalCorridor.Clear();
            unit.PlayerIntentTarget = targetPosition;
            unit.CommandVisualTarget = targetPosition;
            unit.AnchorPosition = targetPosition;
            unit.CommandPulse = 1;
        }

        WavesLaunched++;
        _waveTimer = _profile.AttackWaveInterval;
        LastStatus = $"Enemy wave launched ({_waveUnits.Count} units)";
    }

    private static void CollectAvailableCombatUnits(GameState state, int maximumWaveUnits, List<UnitModel> result)
    {
        result.Clear();
        foreach (var unit in state.Units)
        {
            if (unit.Owner == Owner.Enemy
                && unit.Hp > 0
                && !GameState.IsHarvesterUnit(unit)
                && (unit.AttackTargetId is null || !unit.AttackTargetIsManual))
            {
                result.Add(unit);
            }
        }

        result.Sort(CompareWaveUnits);
        if (result.Count > maximumWaveUnits)
        {
            result.RemoveRange(maximumWaveUnits, result.Count - maximumWaveUnits);
        }
    }

    private static bool TryFindTarget(GameState state, float aggressionRadius, out CombatTargetKind targetKind, out int targetId, out Vector2 targetPosition)
    {
        var enemyCenter = EnemyCenter(state);
        foreach (var building in state.Buildings)
        {
            if (building.Kind == BuildingDesignIds.Headquarters
                && building.Hp > 0
                && state.IsTargetableHostile(Owner.Enemy, building)
                && IsInsideAggressionRadius(building.Position, enemyCenter, aggressionRadius))
            {
                targetKind = CombatTargetKind.Building;
                targetId = building.Id;
                targetPosition = building.Position;
                return true;
            }
        }

        BuildingModel? buildingTarget = null;
        var buildingDistance = float.PositiveInfinity;
        foreach (var building in state.Buildings)
        {
            if (!state.IsTargetableHostile(Owner.Enemy, building)
                || building.Hp <= 0
                || !IsInsideAggressionRadius(building.Position, enemyCenter, aggressionRadius))
            {
                continue;
            }

            var distance = building.Position.DistanceSquaredTo(enemyCenter);
            if (distance < buildingDistance)
            {
                buildingTarget = building;
                buildingDistance = distance;
            }
        }

        if (buildingTarget is not null)
        {
            targetKind = CombatTargetKind.Building;
            targetId = buildingTarget.Id;
            targetPosition = buildingTarget.Position;
            return true;
        }

        UnitModel? unitTarget = null;
        var unitDistance = float.PositiveInfinity;
        foreach (var unit in state.Units)
        {
            if (!state.IsTargetableHostile(Owner.Enemy, unit)
                || unit.Hp <= 0
                || !IsInsideAggressionRadius(unit.Position, enemyCenter, aggressionRadius))
            {
                continue;
            }

            var distance = unit.Position.DistanceSquaredTo(enemyCenter);
            if (distance < unitDistance)
            {
                unitTarget = unit;
                unitDistance = distance;
            }
        }

        if (unitTarget is not null)
        {
            targetKind = CombatTargetKind.Unit;
            targetId = unitTarget.Id;
            targetPosition = unitTarget.Position;
            return true;
        }

        targetKind = CombatTargetKind.Unit;
        targetId = 0;
        targetPosition = Vector2.Zero;
        return false;
    }

    private static bool IsInsideAggressionRadius(Vector2 targetPosition, Vector2 enemyCenter, float aggressionRadius)
    {
        if (float.IsPositiveInfinity(aggressionRadius))
        {
            return true;
        }

        return targetPosition.DistanceSquaredTo(enemyCenter) <= aggressionRadius * aggressionRadius;
    }

    private static Vector2 EnemyCenter(GameState state)
    {
        var sum = Vector2.Zero;
        var count = 0;
        foreach (var unit in state.Units)
        {
            if (unit.Owner == Owner.Enemy && unit.Hp > 0)
            {
                sum += unit.Position;
                count++;
            }
        }

        return count == 0 ? new Vector2(state.WorldSize.X * 0.78f, state.WorldSize.Y * 0.62f) : sum / count;
    }

    private static int CompareWaveUnits(UnitModel left, UnitModel right)
    {
        var byX = left.Position.X.CompareTo(right.Position.X);
        return byX != 0 ? byX : left.Id.CompareTo(right.Id);
    }
}
