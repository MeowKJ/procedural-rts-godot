using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Tools.AiOpponentLoopQa;

internal static partial class AiOpponentLoopQaProgram
{
    private static OpponentLoopRuntime SetupOpponentLoop(TournamentCaseConfig tournamentCase)
    {
        var matchConfig = MatchConfig.Default with
        {
            MapSeed = tournamentCase.Seed,
            PlayerFaction = tournamentCase.LeftFaction,
            AiFaction = tournamentCase.RightFaction,
        };
        var map = SkirmishMapGenerator.GenerateSpec(matchConfig);
        var leftFaction = FactionCatalog.UnitFactionFor(tournamentCase.LeftFaction);
        var rightFaction = FactionCatalog.UnitFactionFor(tournamentCase.RightFaction);
        var leftStart = map.StartFor(new OwnerId(1)).Position.ToVector2();
        var rightStart = map.StartFor(new OwnerId(2)).Position.ToVector2();
        var authorityMap = map with
        {
            Buildings = [],
            Units = [],
        };
        var battlefield = UnitBattlefield.AdoptLoadedMap(MapLoader.Load(authorityMap), authorityMap);
        battlefield.OutcomeViewer = PlayerSlotId.One;
        battlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Hostile);
        battlefield.SetCredits(PlayerSlotId.One, 9000);
        battlefield.SetCredits(PlayerSlotId.Two, 11000);

        var playerBase = BuildRuntimeBase(battlefield, PlayerSlotId.One, leftFaction, leftStart, 0, 100);
        var enemyBase = BuildRuntimeBase(battlefield, PlayerSlotId.Two, rightFaction, rightStart, MathF.PI, 200);
        var resourceFields = battlefield.ResourceFields;
        var enemyResource = resourceFields.MinBy(field => field.Position.DistanceSquaredTo(rightStart))
            ?? throw new InvalidOperationException($"seed {tournamentCase.Seed} generated no enemy resource field");

        SpawnMapRoster(battlefield, map, new OwnerId(2), PlayerSlotId.Two);
        var raiders = SpawnPlayerRaiders(battlefield, leftFaction, rightStart + new Vector2(280, 260));
        var production = new UnitBattlefieldEnemyProductionAi(EnemyDifficultyProfile.Normal with
        {
            ProductionInitialDelay = 0.5f,
            ProductionDecisionInterval = 2.35f,
            DesiredHarvesters = 2,
            MaxQueuedItems = 5,
            AttackInitialDelay = 10f,
            AttackWaveInterval = 18f,
            MinimumWaveUnits = 3,
            MaximumWaveUnits = 8,
            AggressionRadius = float.PositiveInfinity,
        });
        var waves = new UnitBattlefieldEnemyAttackWaveAi(EnemyDifficultyProfile.Normal with
        {
            AttackInitialDelay = 16f,
            AttackWaveInterval = 18f,
            MinimumWaveUnits = 3,
            MaximumWaveUnits = 8,
            AggressionRadius = float.PositiveInfinity,
        });

        return new OpponentLoopRuntime(
            battlefield,
            map,
            playerBase,
            enemyBase,
            resourceFields,
            enemyResource,
            raiders,
            production,
            waves,
            InitialEnemyFieldAmount: enemyResource.Amount,
            InitialPlayerHqHp: playerBase.Headquarters.Hp,
            InitialEnemyCredits: battlefield.Credits(PlayerSlotId.Two),
            InitialRaiderHp: raiders.Sum(unit => unit.Hp),
            SetupFingerprint: SetupFingerprint(map, battlefield));
    }
}

internal sealed record OpponentLoopRuntime(
    UnitBattlefield Battlefield,
    MapSpec Map,
    BaseRuntime PlayerBase,
    BaseRuntime EnemyBase,
    IReadOnlyList<ResourceFieldModel> ResourceFields,
    ResourceFieldModel EnemyResource,
    IReadOnlyList<UnitInstance> Raiders,
    UnitBattlefieldEnemyProductionAi Production,
    UnitBattlefieldEnemyAttackWaveAi Waves,
    int InitialEnemyFieldAmount,
    float InitialPlayerHqHp,
    int InitialEnemyCredits,
    float InitialRaiderHp,
    string SetupFingerprint);
