using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyAttackWaveAi
{
    private const float DefenseCheckInterval = 1.0f;
    private const float DefenseRadius = 900f;

    private readonly EnemyDifficultyProfile _profile;
    private readonly List<UnitInstance> _waveCandidateUnits = new();
    private readonly List<UnitInstance> _waveUnits = new();
    private readonly List<int> _waveUnitIds = new();
    private readonly List<UnitInstance> _defenseUnits = new();
    private readonly List<int> _defenseUnitIds = new();
    private readonly UnitDistanceComparer _unitDistanceComparer = new();
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
        CollectAvailableWaveUnits(
            battlefield,
            enemyPlayerSlotId,
            _profile.MinimumWaveUnits,
            _profile.MaximumWaveUnits,
            _waveUnits);
        if (_waveUnits.Count < _profile.MinimumWaveUnits)
        {
            _waveTimer = 5f;
            LastStatus = $"Enemy wave waiting ({_waveUnits.Count}/{_profile.MinimumWaveUnits})";
            return;
        }

        if (!TryFindTarget(battlefield, enemyPlayerSlotId, _profile.AggressionRadius, out var targetKind, out var targetUnit, out var targetBuilding, out _))
        {
            if (TryIssueScoutWave(battlefield, enemyPlayerSlotId, _waveUnits, _waveUnitIds, out var scoutStatus))
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

        CollectUnitIds(_waveUnits, _waveUnitIds);
        var commanded = targetKind == CombatTargetKind.Building && targetBuilding is { } buildingTarget
            ? battlefield.CommandAttackUnits(enemyPlayerSlotId, _waveUnitIds, buildingTarget.Id)
            : targetUnit is not null
                ? battlefield.CommandAttackUnits(enemyPlayerSlotId, _waveUnitIds, targetUnit)
                : 0;
        if (commanded == 0)
        {
            _waveTimer = 4f;
            LastStatus = "Enemy wave has no valid weapon target";
            return;
        }

        WavesLaunched++;
        _waveTimer = _profile.AttackWaveInterval;
        LastStatus = $"Enemy wave launched ({commanded} units)";
    }

    private bool TryIssueDefenseOrder(UnitBattlefield battlefield, PlayerSlotId playerSlotId, out string status)
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

        CollectAvailableDefenseUnits(battlefield, playerSlotId, targetPosition, 6, _defenseUnits);
        if (_defenseUnits.Count == 0)
        {
            return false;
        }

        CollectUnitIds(_defenseUnits, _defenseUnitIds);
        var commanded = targetKind == CombatTargetKind.Building && targetBuilding is { } buildingTarget
            ? battlefield.CommandAttackUnits(playerSlotId, _defenseUnitIds, buildingTarget.Id)
            : targetUnit is not null
                ? battlefield.CommandAttackUnits(playerSlotId, _defenseUnitIds, targetUnit)
                : 0;
        if (commanded == 0)
        {
            return false;
        }

        status = $"Enemy defense ordered ({commanded} units)";
        return true;
    }
}
