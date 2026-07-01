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
        var enemyHarvesters = state.Units.Count(unit =>
            unit.Owner == Owner.Enemy
            && GameState.IsHarvesterUnit(unit)
            && unit.Hp > 0);
        var queuedHarvesters = state.Buildings
            .Where(building => building.Owner == Owner.Enemy)
            .SelectMany(building => building.ProductionQueue)
            .Count(item => item.Kind == ProductionKind.Harvester);

        if (enemyHarvesters + queuedHarvesters < _profile.DesiredHarvesters && CanQueue(state, ProductionKind.Harvester))
        {
            return ProductionKind.Harvester;
        }

        var combatPreference = _preferTank
            ? new[] { ProductionKind.LightTank, ProductionKind.InfantrySquad }
            : [ProductionKind.InfantrySquad, ProductionKind.LightTank];
        _preferTank = !_preferTank;

        foreach (var kind in combatPreference)
        {
            if (CanQueue(state, kind))
            {
                return kind;
            }
        }

        return CanQueue(state, ProductionKind.Harvester) ? ProductionKind.Harvester : null;
    }

    private static bool CanQueue(GameState state, ProductionKind kind)
    {
        var availableSpecs = ReadyProductionSpecs(state, kind).ToArray();
        return availableSpecs.Length > 0
            && state.Credits(Owner.Enemy) >= availableSpecs.Min(spec => spec.Stats.Cost);
    }

    private static int QueuedCount(GameState state)
    {
        return state.Buildings
            .Where(building => building.Owner == Owner.Enemy)
            .Sum(building => building.ProductionQueue.Count);
    }

    private static void SetEnemyRallyPoints(GameState state)
    {
        var rally = EnemyBaseCenter(state) + new Vector2(-250, -120);
        foreach (var building in state.Buildings.Where(building => building.Owner == Owner.Enemy))
        {
            if (!ProductionKindDesignBridge.PlayableProductionSpecs(ProductionKindDesignBridge.UnitFactionFor(building.FactionId))
                .Any(spec => spec.Production?.ProducerKind == building.Kind))
            {
                continue;
            }

            building.RallyPoint ??= rally;
        }
    }

    private static Vector2 EnemyBaseCenter(GameState state)
    {
        var enemyBuildings = state.Buildings
            .Where(building => building.Owner == Owner.Enemy && building.Hp > 0)
            .ToList();
        if (enemyBuildings.Count == 0)
        {
            return new Vector2(state.WorldSize.X * 0.78f, state.WorldSize.Y * 0.62f);
        }

        return enemyBuildings
            .Select(building => building.Position)
            .Aggregate(Vector2.Zero, (sum, position) => sum + position) / enemyBuildings.Count;
    }

    private static IEnumerable<UnitSpec> ReadyProductionSpecs(GameState state, ProductionKind kind)
    {
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

            yield return spec;
        }
    }
}
