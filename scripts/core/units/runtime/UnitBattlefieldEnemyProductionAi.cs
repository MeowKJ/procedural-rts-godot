namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyProductionAi
{
    private static readonly ProductionCategory[] MixedArmyPlan =
    [
        ProductionCategory.Infantry,
        ProductionCategory.Vehicle,
        ProductionCategory.Infantry,
        ProductionCategory.Air,
        ProductionCategory.Vehicle,
        ProductionCategory.Defense,
    ];

    private readonly EnemyDifficultyProfile _profile;
    private readonly List<ProductionOptionState> _queueableDesignOptions = [];
    private readonly List<UnitBattlefieldBuildingSnapshot> _ownedBuildingBuffer = [];
    private readonly List<UnitInstance> _idleHarvesterBuffer = [];
    private readonly List<int> _idleHarvesterIds = [];
    private float _decisionTimer;
    private int _mixCursor;

    public int SuccessfulOrders { get; private set; }
    public int SuccessfulConstructionOrders { get; private set; }
    public string LastStatus { get; private set; } = "Enemy production AI idle";

    public UnitBattlefieldEnemyProductionAi()
        : this(EnemyDifficultyProfile.Normal)
    {
    }

    public UnitBattlefieldEnemyProductionAi(EnemyDifficultyProfile profile)
    {
        _profile = profile;
        _decisionTimer = profile.ProductionInitialDelay;
    }

    public void Update(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId, double delta)
    {
        _decisionTimer -= (float)delta;
        if (_decisionTimer > 0)
        {
            return;
        }

        _decisionTimer = _profile.ProductionDecisionInterval;
        battlefield.RebuildVisibilityIndex();
        MaintainHarvesterEconomy(battlefield, enemyPlayerSlotId);
        SetEnemyRallyPoints(battlefield, enemyPlayerSlotId);

        if (TryConstructNextBuilding(battlefield, enemyPlayerSlotId, out var constructionStatus))
        {
            SuccessfulConstructionOrders++;
            LastStatus = constructionStatus;
            if (_profile.Difficulty == EnemyDifficulty.Easy)
            {
                return;
            }
        }

        if (QueuedCount(battlefield, enemyPlayerSlotId) >= _profile.MaxQueuedItems)
        {
            LastStatus = "Enemy production queue holding";
            return;
        }

        var nextDesign = ChooseNextProductionDesign(battlefield, enemyPlayerSlotId);
        if (nextDesign is not null && battlefield.CommandEnqueueProductionDesign(nextDesign, enemyPlayerSlotId, out var designStatus))
        {
            SuccessfulOrders++;
            LastStatus = designStatus;
            return;
        }

        LastStatus = "Enemy production waiting for producer or credits";
    }
}
