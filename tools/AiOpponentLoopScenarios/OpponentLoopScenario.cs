using ProceduralRts.Core;
using ProceduralRts.Tools.Qa;

namespace ProceduralRts.Tools.AiOpponentLoopQa;

internal static partial class AiOpponentLoopQaProgram
{
    private static OpponentLoopReport RunOpponentLoop(TournamentCaseConfig tournamentCase)
    {
        var runtime = SetupOpponentLoop(tournamentCase);
        var battlefield = runtime.Battlefield;
        var metrics = new OpponentLoopMetrics(runtime);
        var finalTick = 0;
        int? outcomeTick = null;
        var termination = "tick_limit";
        TournamentStateFailure? stateFailure = null;

        for (var tick = 1; tick <= SimulationTicks; tick++)
        {
            finalTick = tick;
            metrics.CurrentTick = tick;
            ApplyScriptedRaid(runtime, metrics, tick);
            UpdateHarvest(runtime, metrics, tick);
            UpdateProduction(runtime, metrics, tick);
            UpdateAttackWaves(runtime, metrics, tick);

            battlefield.Update(FixedDelta);
            metrics.UpdateAfterTick();
            stateFailure ??= ValidateLiveState(battlefield, tick);
            if (battlefield.Outcome != GameOutcome.InProgress)
            {
                outcomeTick = tick;
                termination = "outcome";
                break;
            }
        }

        return AssembleOpponentLoopReport(
            runtime,
            metrics,
            finalTick,
            termination,
            outcomeTick,
            stateFailure);
    }

    private static void ApplyScriptedRaid(
        OpponentLoopRuntime runtime,
        OpponentLoopMetrics metrics,
        int tick)
    {
        if (metrics.RaidCommanded || tick != 180)
        {
            return;
        }

        var battlefield = runtime.Battlefield;
        var beforeCommands = battlefield.AppliedInputCommandCount;
        battlefield.SelectUnitsByIds(PlayerSlotId.One, runtime.Raiders.Select(unit => unit.Id));
        QaPlayerCommandDriver.AttackBuildingSelection(battlefield, PlayerSlotId.One, runtime.EnemyBase.GroundTurret.Id);
        metrics.LeftAttackCommands += battlefield.AppliedInputCommandCount - beforeCommands;
        metrics.RaidCommanded = true;
    }

    private static void UpdateHarvest(
        OpponentLoopRuntime runtime,
        OpponentLoopMetrics metrics,
        int tick)
    {
        if (tick != 1 && tick % 90 != 0)
        {
            return;
        }

        var battlefield = runtime.Battlefield;
        var beforeCommands = battlefield.AppliedInputCommandCount;
        var assignments = AssignIdleHarvesters(
            battlefield,
            PlayerSlotId.Two,
            runtime.EnemyResource,
            metrics.AssignedHarvesters);
        metrics.HarvestAssignments += assignments;
        metrics.HarvestAppliedCommands += battlefield.AppliedInputCommandCount - beforeCommands;
        if (assignments > 0 && metrics.FirstHarvestTick < 0)
        {
            metrics.FirstHarvestTick = tick;
        }
    }

    private static void UpdateProduction(
        OpponentLoopRuntime runtime,
        OpponentLoopMetrics metrics,
        int tick)
    {
        var battlefield = runtime.Battlefield;
        var production = runtime.Production;
        var previousOrders = production.SuccessfulOrders;
        var previousConstructionOrders = production.SuccessfulConstructionOrders;
        var beforeCommands = battlefield.AppliedInputCommandCount;
        production.Update(battlefield, PlayerSlotId.Two, FixedDelta);
        var commandDelta = battlefield.AppliedInputCommandCount - beforeCommands;
        if (production.SuccessfulConstructionOrders > previousConstructionOrders)
        {
            metrics.ConstructionAppliedCommands += commandDelta;
            metrics.FirstConstructionTick = metrics.FirstConstructionTick < 0 ? tick : metrics.FirstConstructionTick;
        }

        if (production.SuccessfulOrders > previousOrders)
        {
            metrics.ProductionAppliedCommands += commandDelta;
        }
    }

    private static void UpdateAttackWaves(
        OpponentLoopRuntime runtime,
        OpponentLoopMetrics metrics,
        int tick)
    {
        var battlefield = runtime.Battlefield;
        var waves = runtime.Waves;
        var previousWaves = waves.WavesLaunched;
        var beforeCommands = battlefield.AppliedInputCommandCount;
        waves.Update(battlefield, PlayerSlotId.Two, FixedDelta);
        if (waves.WavesLaunched <= previousWaves)
        {
            return;
        }

        metrics.WaveAppliedCommands += battlefield.AppliedInputCommandCount - beforeCommands;
        metrics.LaunchedWaveUnitOrders += battlefield.Units.Count(unit =>
            unit.PlayerSlotId == PlayerSlotId.Two
            && unit.AttackTargetIsManual
            && unit.AttackTargetKind == CombatTargetKind.Building
            && unit.AttackTargetId == runtime.PlayerBase.Headquarters.Id);
        if (metrics.FirstWaveTick < 0)
        {
            metrics.FirstWaveTick = tick;
        }
        else if (metrics.SecondWaveTick < 0)
        {
            metrics.SecondWaveTick = tick;
        }
    }
}
