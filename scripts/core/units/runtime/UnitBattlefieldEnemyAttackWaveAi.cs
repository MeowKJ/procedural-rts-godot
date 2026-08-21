using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyAttackWaveAi
{
    private const float DefenseCheckInterval = 1.0f;
    private const float DefenseRadius = 900f;

    private readonly EnemyDifficultyProfile _profile;
    private readonly List<UnitInstance> _waveCandidateUnits = new();
    private readonly List<UnitInstance> _waveUnits = new();
    private readonly List<EntityId> _waveEntityIds = new();
    private readonly List<UnitInstance> _defenseUnits = new();
    private readonly List<EntityId> _defenseEntityIds = new();
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
            if (TryIssueScoutWave(battlefield, enemyPlayerSlotId, _waveUnits, _waveEntityIds, out var scoutStatus))
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

        CollectEntityIds(_waveUnits, _waveEntityIds);
        var commanded = targetKind == CombatTargetKind.Building && targetBuilding is { } buildingTarget
            ? SubmitAttackCommand(battlefield, enemyPlayerSlotId, _waveEntityIds, buildingTarget)
            : targetUnit is not null
                ? SubmitAttackCommand(battlefield, enemyPlayerSlotId, _waveEntityIds, targetUnit)
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

        CollectEntityIds(_defenseUnits, _defenseEntityIds);
        var commanded = targetKind == CombatTargetKind.Building && targetBuilding is { } buildingTarget
            ? SubmitAttackCommand(battlefield, playerSlotId, _defenseEntityIds, buildingTarget)
            : targetUnit is not null
                ? SubmitAttackCommand(battlefield, playerSlotId, _defenseEntityIds, targetUnit)
                : 0;
        if (commanded == 0)
        {
            return false;
        }

        status = $"Enemy defense ordered ({commanded} units)";
        return true;
    }

    private static int SubmitAttackCommand(
        UnitBattlefield battlefield,
        PlayerSlotId playerSlotId,
        IReadOnlyList<EntityId> subjects,
        UnitInstance target)
    {
        var result = UnitBattlefieldScriptedCommandDriver.Submit(
            battlefield,
            "enemy-attack",
            playerSlotId,
            PlayerCommandKind.Attack,
            PlayerCommandPayload.ForEntityTarget(subjects, target.EntityId));
        return result.AcceptedCount == 1 ? subjects.Count : 0;
    }

    private static int SubmitAttackCommand(
        UnitBattlefield battlefield,
        PlayerSlotId playerSlotId,
        IReadOnlyList<EntityId> subjects,
        UnitBattlefieldBuildingSnapshot target)
    {
        if (battlefield.BuildingEntityIdByTargetId(target.Id) is not { } targetEntityId)
        {
            return 0;
        }

        var result = UnitBattlefieldScriptedCommandDriver.Submit(
            battlefield,
            "enemy-attack",
            playerSlotId,
            PlayerCommandKind.Attack,
            PlayerCommandPayload.ForEntityTarget(subjects, targetEntityId, CombatTargetKind.Building));
        return result.AcceptedCount == 1 ? subjects.Count : 0;
    }
}
