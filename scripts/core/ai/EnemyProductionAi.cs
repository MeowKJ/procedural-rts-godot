using Godot;

namespace ProceduralRts.Core;

public sealed class EnemyProductionAi
{
    private readonly EnemyDifficultyProfile _profile;
    private float _decisionTimer;
    private bool _preferTank = true;

    public int SuccessfulOrders { get; private set; }
    public string LastStatus { get; private set; } = "Enemy production AI idle";

    public EnemyProductionAi()
        : this(EnemyDifficultyProfile.Normal)
    {
    }

    public EnemyProductionAi(EnemyDifficultyProfile profile)
    {
        _profile = profile;
        _decisionTimer = profile.ProductionInitialDelay;
    }

    public void Update(GameState state, double delta)
    {
        _decisionTimer -= (float)delta;
        if (_decisionTimer > 0)
        {
            return;
        }

        _decisionTimer = _profile.ProductionDecisionInterval;
        SetEnemyRallyPoints(state);

        if (state.QueuedProductionCount(Owner.Enemy) >= _profile.MaxQueuedItems)
        {
            LastStatus = "Enemy production queue holding";
            return;
        }

        var next = ChooseNextProduction(state);
        if (next is null)
        {
            LastStatus = "Enemy production waiting for producer or credits";
            return;
        }

        if (state.CommandEnqueueProduction(next.Value, Owner.Enemy, out var status))
        {
            SuccessfulOrders++;
        }

        LastStatus = status;
    }

    private ProductionKind? ChooseNextProduction(GameState state)
    {
        var enemyHarvesters = state.LiveHarvesterCount(Owner.Enemy);
        var queuedHarvesters = state.QueuedProductionCount(Owner.Enemy, ProductionKind.Harvester);

        if (enemyHarvesters + queuedHarvesters < _profile.DesiredHarvesters && CanQueue(state, ProductionKind.Harvester))
        {
            return ProductionKind.Harvester;
        }

        var preferTank = _preferTank;
        _preferTank = !_preferTank;
        var firstCombat = preferTank ? ProductionKind.LightTank : ProductionKind.InfantrySquad;
        var secondCombat = preferTank ? ProductionKind.InfantrySquad : ProductionKind.LightTank;
        if (CanQueue(state, firstCombat))
        {
            return firstCombat;
        }

        if (CanQueue(state, secondCombat))
        {
            return secondCombat;
        }

        return CanQueue(state, ProductionKind.Harvester) ? ProductionKind.Harvester : null;
    }

    private static bool CanQueue(GameState state, ProductionKind kind)
    {
        return state.CanQueueProduction(kind, Owner.Enemy);
    }

    private static void SetEnemyRallyPoints(GameState state)
    {
        var fallback = new Vector2(state.WorldSize.X * 0.78f, state.WorldSize.Y * 0.62f);
        var rally = state.LiveBuildingCenter(Owner.Enemy, fallback) + new Vector2(-250, -120);
        state.CommandSetProducerRallyPoints(Owner.Enemy, rally);
    }

}
