using Godot;

namespace ProceduralRts.Core;

public sealed class EnemyAttackWaveAi
{
    private readonly EnemyDifficultyProfile _profile;
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

        var waveUnits = AvailableCombatUnits(state, _profile.MaximumWaveUnits).ToList();
        if (waveUnits.Count < _profile.MinimumWaveUnits)
        {
            _waveTimer = 5f;
            LastStatus = $"Enemy wave waiting ({waveUnits.Count}/{_profile.MinimumWaveUnits})";
            return;
        }

        if (!TryFindTarget(state, _profile.AggressionRadius, out var targetKind, out var targetId, out var targetPosition))
        {
            _waveTimer = 8f;
            LastStatus = "Enemy wave has no target";
            return;
        }

        foreach (var unit in waveUnits)
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
        LastStatus = $"Enemy wave launched ({waveUnits.Count} units)";
    }

    private static IEnumerable<UnitModel> AvailableCombatUnits(GameState state, int maximumWaveUnits)
    {
        return state.Units
            .Where(unit => unit.Owner == Owner.Enemy)
            .Where(unit => unit.Hp > 0)
            .Where(unit => !GameState.IsHarvesterUnit(unit))
            .Where(unit => unit.AttackTargetId is null || !unit.AttackTargetIsManual)
            .OrderBy(unit => unit.Position.X)
            .ThenBy(unit => unit.Id)
            .Take(maximumWaveUnits);
    }

    private static bool TryFindTarget(GameState state, float aggressionRadius, out CombatTargetKind targetKind, out int targetId, out Vector2 targetPosition)
    {
        var enemyCenter = EnemyCenter(state);
        var hq = state.Buildings
            .Where(building => state.IsTargetableHostile(Owner.Enemy, building))
            .Where(building => building.Hp > 0)
            .FirstOrDefault(building => building.Kind == BuildingDesignIds.Headquarters);
        if (hq is not null && IsInsideAggressionRadius(hq.Position, enemyCenter, aggressionRadius))
        {
            targetKind = CombatTargetKind.Building;
            targetId = hq.Id;
            targetPosition = hq.Position;
            return true;
        }

        var buildingTarget = state.Buildings
            .Where(building => state.IsTargetableHostile(Owner.Enemy, building) && building.Hp > 0)
            .Where(building => IsInsideAggressionRadius(building.Position, enemyCenter, aggressionRadius))
            .OrderBy(building => building.Position.DistanceSquaredTo(enemyCenter))
            .FirstOrDefault();
        if (buildingTarget is not null)
        {
            targetKind = CombatTargetKind.Building;
            targetId = buildingTarget.Id;
            targetPosition = buildingTarget.Position;
            return true;
        }

        var unitTarget = state.Units
            .Where(unit => state.IsTargetableHostile(Owner.Enemy, unit) && unit.Hp > 0)
            .Where(unit => IsInsideAggressionRadius(unit.Position, enemyCenter, aggressionRadius))
            .OrderBy(unit => unit.Position.DistanceSquaredTo(enemyCenter))
            .FirstOrDefault();
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
        var units = state.Units
            .Where(unit => unit.Owner == Owner.Enemy && unit.Hp > 0)
            .Select(unit => unit.Position)
            .ToList();
        if (units.Count == 0)
        {
            return new Vector2(state.WorldSize.X * 0.78f, state.WorldSize.Y * 0.62f);
        }

        return units.Aggregate(Vector2.Zero, (sum, position) => sum + position) / units.Count;
    }
}
