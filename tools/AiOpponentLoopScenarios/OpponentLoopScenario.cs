using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Tools.AiOpponentLoopQa;

internal static partial class AiOpponentLoopQaProgram
{
    private static OpponentLoopReport RunOpponentLoop()
    {
        var battlefield = new UnitBattlefield
        {
            WorldSize = new Vector2(3600, 2400),
            OutcomeViewer = PlayerSlotId.One,
        };
        battlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Hostile);
        battlefield.SetCredits(PlayerSlotId.One, 9000);
        battlefield.SetCredits(PlayerSlotId.Two, 11000);

        var playerBase = BuildRuntimeBase(
            battlefield,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(780, 1200),
            facing: 0,
            idBase: 100);
        var enemyBase = BuildRuntimeBase(
            battlefield,
            PlayerSlotId.Two,
            UnitFactionId.Cat,
            new Vector2(2820, 1200),
            facing: MathF.PI,
            idBase: 200);

        var enemyResource = ResourceField(1, new Vector2(2990, 970), 18000);
        var playerResource = ResourceField(2, new Vector2(610, 980), 9000);
        battlefield.SetResourceFields([enemyResource, playerResource]);

        var initialEnemyFieldAmount = enemyResource.Amount;
        var initialPlayerHqHp = playerBase.Headquarters.Hp;
        var initialEnemyCredits = battlefield.Credits(PlayerSlotId.Two);

        SpawnStartingRoster(battlefield, PlayerSlotId.Two, UnitFactionId.Cat, new Vector2(2690, 1200), direction: -1);
        var raiders = SpawnPlayerRaiders(battlefield, new Vector2(2420, 1190));

        battlefield.SelectUnitsByIds(PlayerSlotId.One, raiders.Select(unit => unit.Id));
        battlefield.CommandAttackSelected(PlayerSlotId.One, enemyBase.GroundTurret.Id);

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

        var queuedEvents = 0;
        var completedEvents = 0;
        var producedDesignIds = new List<string>();
        var producedCombatDesignIds = new List<string>();
        var defenseBuildingHits = 0;
        var defenseUnitHits = 0;
        var raiderDeaths = 0;
        var enemyBuildingHits = 0;
        var resourceEvents = 0;
        var harvestAssignments = 0;
        var harvestBridgeCommands = 0;
        var productionBridgeCommands = 0;
        var waveBridgeCommands = 0;
        var launchedWaveUnitOrders = 0;
        var maxManualWaveAttackers = 0;
        var maxEnemyCombatUnitsAlive = battlefield.Units.Count(unit => IsCombat(unit, PlayerSlotId.Two));
        var minRaiderHp = raiders.Sum(unit => MathF.Max(0, unit.Hp));
        var maxEnemyCredits = initialEnemyCredits;
        var firstWaveTick = -1;
        var secondWaveTick = -1;
        var assignedHarvesters = new HashSet<int>();
        var raiderIds = raiders.Select(unit => unit.Id).ToHashSet();

        battlefield.ProductionQueued += (building, _) =>
        {
            if (building.PlayerSlotId == PlayerSlotId.Two)
            {
                queuedEvents++;
            }
        };
        battlefield.ProductionCompleted += (building, _, unit) =>
        {
            if (building.PlayerSlotId != PlayerSlotId.Two || unit.PlayerSlotId != PlayerSlotId.Two)
            {
                return;
            }

            completedEvents++;
            producedDesignIds.Add(unit.Spec.Id);
            if (!unit.Spec.RoleTags.Contains(UnitRoleTag.Economy))
            {
                producedCombatDesignIds.Add(unit.Spec.Id);
            }
        };
        battlefield.UnitAttacked += (target, attacker) =>
        {
            if (target.PlayerSlotId == PlayerSlotId.One && attacker.PlayerSlotId == PlayerSlotId.Two)
            {
                defenseUnitHits++;
            }
        };
        battlefield.UnitAttackedByBuilding += (target, attacker) =>
        {
            if (target.PlayerSlotId == PlayerSlotId.One && attacker.PlayerSlotId == PlayerSlotId.Two)
            {
                defenseBuildingHits++;
            }
        };
        battlefield.BuildingAttacked += (target, attacker) =>
        {
            if (target.PlayerSlotId == PlayerSlotId.One && attacker.PlayerSlotId == PlayerSlotId.Two)
            {
                enemyBuildingHits++;
            }
        };
        battlefield.UnitsRemoved += deaths =>
        {
            raiderDeaths += deaths.Count(death => raiderIds.Contains(death.Id));
        };
        battlefield.ResourceInventoryChanged += (slot, _) =>
        {
            if (slot == PlayerSlotId.Two)
            {
                resourceEvents++;
            }
        };

        for (var tick = 1; tick <= SimulationTicks && battlefield.Outcome == GameOutcome.InProgress; tick++)
        {
            if (tick == 1 || tick % 90 == 0)
            {
                var beforeHarvestCommands = battlefield.AppliedInputCommandCount;
                harvestAssignments += AssignIdleHarvesters(battlefield, PlayerSlotId.Two, enemyResource, assignedHarvesters);
                harvestBridgeCommands += battlefield.AppliedInputCommandCount - beforeHarvestCommands;
            }

            var previousOrders = production.SuccessfulOrders;
            var beforeProductionCommands = battlefield.AppliedInputCommandCount;
            production.Update(battlefield, PlayerSlotId.Two, FixedDelta);
            if (production.SuccessfulOrders > previousOrders)
            {
                productionBridgeCommands += battlefield.AppliedInputCommandCount - beforeProductionCommands;
            }

            var previousWaves = waves.WavesLaunched;
            var beforeWaveCommands = battlefield.AppliedInputCommandCount;
            waves.Update(battlefield, PlayerSlotId.Two, FixedDelta);
            if (waves.WavesLaunched > previousWaves)
            {
                var commanded = battlefield.AppliedInputCommandCount - beforeWaveCommands;
                waveBridgeCommands += commanded;
                launchedWaveUnitOrders += battlefield.Units.Count(unit =>
                    unit.PlayerSlotId == PlayerSlotId.Two
                    && unit.AttackTargetIsManual
                    && unit.AttackTargetKind == CombatTargetKind.Building
                    && unit.AttackTargetId == playerBase.Headquarters.Id);
                if (firstWaveTick < 0)
                {
                    firstWaveTick = tick;
                }
                else if (secondWaveTick < 0)
                {
                    secondWaveTick = tick;
                }
            }

            battlefield.Update(FixedDelta);
            maxEnemyCredits = Math.Max(maxEnemyCredits, battlefield.Credits(PlayerSlotId.Two));
            maxEnemyCombatUnitsAlive = Math.Max(maxEnemyCombatUnitsAlive, battlefield.Units.Count(unit => IsCombat(unit, PlayerSlotId.Two)));
            maxManualWaveAttackers = Math.Max(
                maxManualWaveAttackers,
                battlefield.Units.Count(unit =>
                    unit.PlayerSlotId == PlayerSlotId.Two
                    && unit.AttackTargetIsManual
                    && unit.AttackTargetKind == CombatTargetKind.Building
                    && unit.AttackTargetId == playerBase.Headquarters.Id));
            minRaiderHp = Math.Min(minRaiderHp, battlefield.Units
                .Where(unit => raiderIds.Contains(unit.Id))
                .Sum(unit => MathF.Max(0, unit.Hp)));
        }

        var producedCombatSpecs = producedCombatDesignIds
            .Select(UnitDesignCatalog.Spec)
            .ToList();
        var producedInfantry = producedCombatSpecs.Count(spec => spec.RoleTags.Contains(UnitRoleTag.Infantry));
        var producedVehicles = producedCombatSpecs.Count(spec => spec.RoleTags.Contains(UnitRoleTag.Vehicle));
        var enemyCombatAlive = battlefield.Units.Count(unit => IsCombat(unit, PlayerSlotId.Two));
        var playerHqHp = Math.Max(0, battlefield.BuildingSnapshot(playerBase.Headquarters.Id)?.Hp ?? 0);
        var enemyFieldDepleted = initialEnemyFieldAmount - enemyResource.Amount;
        var totalCommands = battlefield.AppliedInputCommandCount;

        return new OpponentLoopReport(
            SimulationSeconds: SimulationTicks / 30,
            ProductionOrders: production.SuccessfulOrders,
            ProductionQueuedEvents: queuedEvents,
            ProductionCompletedEvents: completedEvents,
            ProducedDesignIds: producedDesignIds.Distinct().OrderBy(id => id).ToArray(),
            ProducedCombatDesignIds: producedCombatDesignIds.Distinct().OrderBy(id => id).ToArray(),
            ProducedInfantry: producedInfantry,
            ProducedVehicles: producedVehicles,
            HarvestAssignments: harvestAssignments,
            EnemyFieldDepleted: enemyFieldDepleted,
            EnemyCreditsStart: initialEnemyCredits,
            EnemyCreditsPeak: maxEnemyCredits,
            ResourceEvents: resourceEvents,
            BuiltEnemyBuildingSpecIds: battlefield.BuildingSnapshots()
                .Where(building => building.PlayerSlotId == PlayerSlotId.Two)
                .Select(building => building.Kind)
                .Distinct()
                .OrderBy(kind => kind)
                .ToArray(),
            DefenseBuildingHits: defenseBuildingHits,
            DefenseUnitHits: defenseUnitHits,
            RaiderDeaths: raiderDeaths,
            RaiderHpDamage: raiders.Count * UnitDesignCatalog.Spec("dog.patrol_vehicle").Stats.MaxHp - minRaiderHp,
            WavesLaunched: waves.WavesLaunched,
            FirstWaveTick: firstWaveTick,
            SecondWaveTick: secondWaveTick,
            MaxManualWaveAttackers: maxManualWaveAttackers,
            LaunchedWaveUnitOrders: launchedWaveUnitOrders,
            EnemyBuildingHitsOnPlayerBase: enemyBuildingHits,
            PlayerHqDamage: initialPlayerHqHp - playerHqHp,
            EnemyCombatUnitsAlive: enemyCombatAlive,
            MaxEnemyCombatUnitsAlive: maxEnemyCombatUnitsAlive,
            TotalAppliedCommands: totalCommands,
            HarvestBridgeCommands: harvestBridgeCommands,
            ProductionBridgeCommands: productionBridgeCommands,
            WaveBridgeCommands: waveBridgeCommands,
            ProductionStatus: production.LastStatus,
            WaveStatus: waves.LastStatus,
            Outcome: battlefield.Outcome);
    }
}
