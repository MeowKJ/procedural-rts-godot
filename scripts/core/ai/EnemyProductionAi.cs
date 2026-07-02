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

        if (QueuedCount(state) >= _profile.MaxQueuedItems)
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

        if (state.EnqueueProduction(next.Value, Owner.Enemy, out var status))
        {
            SuccessfulOrders++;
        }

        LastStatus = status;
    }

    private ProductionKind? ChooseNextProduction(GameState state)
    {
        var enemyHarvesters = 0;
        foreach (var unit in state.Units)
        {
            if (unit.Owner == Owner.Enemy
                && GameState.IsHarvesterUnit(unit)
                && unit.Hp > 0)
            {
                enemyHarvesters++;
            }
        }

        var queuedHarvesters = 0;
        foreach (var building in state.Buildings)
        {
            if (building.Owner == Owner.Enemy)
            {
                foreach (var item in building.ProductionQueue)
                {
                    if (item.Kind == ProductionKind.Harvester)
                    {
                        queuedHarvesters++;
                    }
                }
            }
        }

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
        return TryMinReadyProductionCost(state, kind, out var minCost)
            && state.Credits(Owner.Enemy) >= minCost;
    }

    private static int QueuedCount(GameState state)
    {
        var count = 0;
        foreach (var building in state.Buildings)
        {
            if (building.Owner == Owner.Enemy)
            {
                count += building.ProductionQueue.Count;
            }
        }

        return count;
    }

    private static void SetEnemyRallyPoints(GameState state)
    {
        var rally = EnemyBaseCenter(state) + new Vector2(-250, -120);
        foreach (var building in state.Buildings)
        {
            if (building.Owner != Owner.Enemy
                || !HasPlayableProducerKind(building))
            {
                continue;
            }

            building.RallyPoint ??= rally;
        }
    }

    private static Vector2 EnemyBaseCenter(GameState state)
    {
        var sum = Vector2.Zero;
        var count = 0;
        foreach (var building in state.Buildings)
        {
            if (building.Owner != Owner.Enemy || building.Hp <= 0)
            {
                continue;
            }

            sum += building.Position;
            count++;
        }

        return count == 0 ? new Vector2(state.WorldSize.X * 0.78f, state.WorldSize.Y * 0.62f) : sum / count;
    }

    private static bool TryMinReadyProductionCost(GameState state, ProductionKind kind, out int minCost)
    {
        minCost = int.MaxValue;
        var found = false;
        foreach (var building in state.Buildings)
        {
            if (!ProductionKindDesignBridge.TrySpecFor(building.FactionId, kind, out var spec)
                || spec.Production is not { } production)
            {
                continue;
            }

            if (building.Owner != Owner.Enemy
                || building.Hp <= 0
                || !building.Powered
                || building.BuildProgress < 1
                || production.ProducerKind != building.Kind)
            {
                continue;
            }

            minCost = Math.Min(minCost, spec.Stats.Cost);
            found = true;
        }

        return found;
    }

    private static bool HasPlayableProducerKind(BuildingModel building)
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
}
