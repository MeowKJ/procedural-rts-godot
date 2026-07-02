using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyAttackWaveAi
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
}
