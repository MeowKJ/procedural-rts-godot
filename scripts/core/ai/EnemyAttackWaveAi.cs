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

        if (!TryFindTarget(state, _profile.AggressionRadius, out var targetKind, out var targetId, out _))
        {
            _waveTimer = 8f;
            LastStatus = "Enemy wave has no target";
            return;
        }

        var commanded = state.CommandAttackUnits(_waveUnits, targetKind, targetId);
        if (commanded == 0)
        {
            _waveTimer = 5f;
            LastStatus = "Enemy wave has no valid attackers";
            return;
        }

        WavesLaunched++;
        _waveTimer = _profile.AttackWaveInterval;
        LastStatus = $"Enemy wave launched ({commanded} units)";
    }

    private static void CollectAvailableCombatUnits(GameState state, int maximumWaveUnits, List<UnitModel> result)
    {
        state.CollectAvailableAttackWaveUnits(Owner.Enemy, result);
        result.Sort(CompareWaveUnits);
        if (result.Count > maximumWaveUnits)
        {
            result.RemoveRange(maximumWaveUnits, result.Count - maximumWaveUnits);
        }
    }

    private static bool TryFindTarget(GameState state, float aggressionRadius, out CombatTargetKind targetKind, out int targetId, out Vector2 targetPosition)
    {
        return state.TryFindAttackWaveTarget(Owner.Enemy, EnemyFallbackCenter(state), aggressionRadius, out targetKind, out targetId, out targetPosition);
    }

    private static Vector2 EnemyFallbackCenter(GameState state)
    {
        return new Vector2(state.WorldSize.X * 0.78f, state.WorldSize.Y * 0.62f);
    }

    private static int CompareWaveUnits(UnitModel left, UnitModel right)
    {
        var byX = left.Position.X.CompareTo(right.Position.X);
        return byX != 0 ? byX : left.Id.CompareTo(right.Id);
    }
}
