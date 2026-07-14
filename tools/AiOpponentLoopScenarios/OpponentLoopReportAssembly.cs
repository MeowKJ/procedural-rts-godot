using ProceduralRts.Core;

namespace ProceduralRts.Tools.AiOpponentLoopQa;

internal static partial class AiOpponentLoopQaProgram
{
    private static OpponentLoopReport AssembleOpponentLoopReport(
        OpponentLoopRuntime runtime,
        OpponentLoopMetrics metrics,
        int finalTick,
        string termination,
        int? outcomeTick,
        TournamentStateFailure? stateFailure)
    {
        var battlefield = runtime.Battlefield;
        var producedCombatSpecs = metrics.ProducedCombatDesignIds.Select(UnitDesignCatalog.Spec).ToArray();
        var finalBuildings = battlefield.BuildingSnapshots();
        var milestones = new[]
        {
            metrics.FirstHarvestTick,
            metrics.FirstConstructionTick,
            metrics.FirstProductionTick,
            metrics.FirstEngagementTick,
        };

        return new OpponentLoopReport(
            FinalTick: finalTick,
            Termination: termination,
            OutcomeTick: outcomeTick,
            SimulationSeconds: finalTick * FixedDelta,
            SetupFingerprint: runtime.SetupFingerprint,
            ProductionOrders: runtime.Production.SuccessfulOrders,
            ProductionQueuedEvents: metrics.QueuedEvents,
            ProductionCompletedEvents: metrics.CompletedEvents,
            ProducedDesignIds: metrics.ProducedDesignIds.Distinct().OrderBy(id => id).ToArray(),
            ProducedCombatDesignIds: metrics.ProducedCombatDesignIds.Distinct().OrderBy(id => id).ToArray(),
            ProducedInfantry: producedCombatSpecs.Count(spec => spec.RoleTags.Contains(UnitRoleTag.Infantry)),
            ProducedVehicles: producedCombatSpecs.Count(spec => spec.RoleTags.Contains(UnitRoleTag.Vehicle)),
            HarvestAssignments: metrics.HarvestAssignments,
            EnemyFieldDepleted: runtime.InitialEnemyFieldAmount - runtime.EnemyResource.Amount,
            EnemyCreditsStart: runtime.InitialEnemyCredits,
            EnemyCreditsPeak: metrics.MaxEnemyCredits,
            ResourceEvents: metrics.ResourceEvents,
            ConstructionOrders: runtime.Production.SuccessfulConstructionOrders,
            BuiltEnemyBuildingSpecIds: finalBuildings
                .Where(building => building.PlayerSlotId == PlayerSlotId.Two)
                .Select(building => building.Kind)
                .Distinct()
                .OrderBy(kind => kind)
                .ToArray(),
            DefenseBuildingHits: metrics.DefenseBuildingHits,
            DefenseUnitHits: metrics.DefenseUnitHits,
            RaiderDeaths: metrics.RaiderDeaths,
            RaiderHpDamage: runtime.InitialRaiderHp - metrics.MinRaiderHp,
            WavesLaunched: runtime.Waves.WavesLaunched,
            FirstWaveTick: metrics.FirstWaveTick,
            SecondWaveTick: metrics.SecondWaveTick,
            MaxManualWaveAttackers: metrics.MaxManualWaveAttackers,
            LaunchedWaveUnitOrders: metrics.LaunchedWaveUnitOrders,
            EnemyBuildingHitsOnPlayerBase: metrics.EnemyBuildingHits,
            PlayerHqDamage: runtime.InitialPlayerHqHp
                - Math.Max(0, battlefield.BuildingSnapshot(runtime.PlayerBase.Headquarters.Id)?.Hp ?? 0),
            EnemyCombatUnitsAlive: battlefield.Units.Count(unit => IsCombat(unit, PlayerSlotId.Two)),
            MaxEnemyCombatUnitsAlive: metrics.MaxEnemyCombatUnitsAlive,
            TotalAppliedCommands: battlefield.AppliedInputCommandCount,
            HarvestBridgeCommands: metrics.HarvestBridgeCommands,
            ConstructionBridgeCommands: metrics.ConstructionBridgeCommands,
            ProductionBridgeCommands: metrics.ProductionBridgeCommands,
            WaveBridgeCommands: metrics.WaveBridgeCommands,
            LeftAttackCommands: metrics.LeftAttackCommands,
            LeftToRightDamage: metrics.LeftToRightDamage,
            RightToLeftDamage: metrics.RightToLeftDamage,
            LeftFinalUnitCount: battlefield.Units.Count(unit => unit.PlayerSlotId == PlayerSlotId.One),
            RightFinalUnitCount: battlefield.Units.Count(unit => unit.PlayerSlotId == PlayerSlotId.Two),
            LeftFinalBuildingCount: finalBuildings.Count(building => building.PlayerSlotId == PlayerSlotId.One),
            RightFinalBuildingCount: finalBuildings.Count(building => building.PlayerSlotId == PlayerSlotId.Two),
            ProductionStatus: runtime.Production.LastStatus,
            WaveStatus: runtime.Waves.LastStatus,
            Outcome: battlefield.Outcome,
            FirstHarvestTick: metrics.FirstHarvestTick,
            FirstConstructionTick: metrics.FirstConstructionTick,
            FirstProductionTick: metrics.FirstProductionTick,
            FirstEngagementTick: metrics.FirstEngagementTick,
            MilestoneCompletionTick: milestones.Any(tick => tick < 0) ? finalTick + 1 : milestones.Max(),
            StateFailure: stateFailure);
    }
}
