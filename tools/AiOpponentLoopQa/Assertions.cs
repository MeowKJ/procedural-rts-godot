using ProceduralRts.Core;

namespace ProceduralRts.Tools.AiOpponentLoopQa;

internal static partial class AiOpponentLoopQaProgram
{
    private static void AssertOpponentLoop(OpponentLoopReport report, List<string> failures)
    {
        if (report.FinalTick <= 0 || report.FinalTick > SimulationTicks)
        {
            failures.Add($"case final tick should be within 1..{SimulationTicks}; got {report.FinalTick}.");
        }

        if (report.Termination == "outcome")
        {
            if (report.Outcome == GameOutcome.InProgress || report.OutcomeTick != report.FinalTick)
            {
                failures.Add($"outcome termination should record the terminal outcome tick; outcome {report.Outcome}, outcome/final {report.OutcomeTick}/{report.FinalTick}.");
            }
        }
        else if (report.Termination != "tick_limit"
            || report.FinalTick != SimulationTicks
            || report.Outcome != GameOutcome.InProgress
            || report.OutcomeTick is not null)
        {
            failures.Add($"tick-limit termination should end in progress at tick {SimulationTicks}; got {report.Termination}/{report.FinalTick}/{report.Outcome}/{report.OutcomeTick}.");
        }

        if (report.StateFailure is not null)
        {
            failures.Add($"case should keep every live state finite and in bounds; {report.StateFailure.Code}/{report.StateFailure.Subject}@{report.StateFailure.Tick}.");
        }

        if (report.FirstHarvestTick < 0
            || report.FirstConstructionTick < 0
            || report.FirstProductionTick < 0
            || report.FirstEngagementTick < 0
            || report.MilestoneCompletionTick > 30 * 60)
        {
            failures.Add(
                $"case milestones stalled; harvest/build/production/engagement ticks "
                + $"{report.FirstHarvestTick}/{report.FirstConstructionTick}/{report.FirstProductionTick}/{report.FirstEngagementTick}, "
                + $"completion {report.MilestoneCompletionTick}.");
        }

        if (report.HarvestAssignments < 1 || report.EnemyFieldDepleted <= 0)
        {
            failures.Add($"AI harvest loop should assign harvesters and deplete resources; assignments {report.HarvestAssignments}, depleted {report.EnemyFieldDepleted}.");
        }

        if (report.HarvestBridgeCommands < 2)
        {
            failures.Add($"AI harvest should enter through selection/harvest command bridge; command delta {report.HarvestBridgeCommands}.");
        }

        var requiredBuildings = new[] { BuildingDesignIds.Headquarters, BuildingDesignIds.Refinery, BuildingDesignIds.Barracks, BuildingDesignIds.VehicleFactory, BuildingDesignIds.GroundTurret };
        foreach (var kind in requiredBuildings)
        {
            if (!report.BuiltEnemyBuildingSpecIds.Contains(kind))
            {
                failures.Add($"AI base should expose built {kind} for the opponent loop.");
            }
        }

        if (report.ConstructionOrders < 2)
        {
            failures.Add($"AI construction should place multiple runtime buildings; construction orders {report.ConstructionOrders}.");
        }

        if (report.ConstructionBridgeCommands < report.ConstructionOrders)
        {
            failures.Add($"AI construction should enter through the UnitBattlefield construction command bridge; bridge/orders = {report.ConstructionBridgeCommands}/{report.ConstructionOrders}.");
        }

        if (report.ProductionOrders < 5 || report.ProductionQueuedEvents < 5 || report.ProductionCompletedEvents < 3)
        {
            failures.Add($"AI production should queue and complete multiple units; orders/queued/completed = {report.ProductionOrders}/{report.ProductionQueuedEvents}/{report.ProductionCompletedEvents}.");
        }

        if (report.ProducedInfantry < 1 || report.ProducedVehicles < 1)
        {
            failures.Add($"AI should produce a mixed combat army; produced infantry/vehicles = {report.ProducedInfantry}/{report.ProducedVehicles} [{string.Join(", ", report.ProducedCombatDesignIds)}].");
        }

        if (report.ProductionBridgeCommands < report.ProductionOrders)
        {
            failures.Add($"AI production orders should advance the EntityWorld production command bridge; bridge/orders = {report.ProductionBridgeCommands}/{report.ProductionOrders}.");
        }

        if (report.DefenseBuildingHits + report.DefenseUnitHits <= 0 || report.RaiderHpDamage <= 0)
        {
            failures.Add($"AI should defend against the player raid; defense hits building/unit = {report.DefenseBuildingHits}/{report.DefenseUnitHits}, raider damage {report.RaiderHpDamage:0}.");
        }

        if (report.WavesLaunched < 2 || report.MaxManualWaveAttackers < 3 || report.PlayerHqDamage <= 0)
        {
            failures.Add($"AI should attack in repeated waves and damage the player HQ; waves {report.WavesLaunched}, attackers {report.MaxManualWaveAttackers}, HQ damage {report.PlayerHqDamage:0}.");
        }

        if (report.WaveBridgeCommands < report.WavesLaunched)
        {
            failures.Add($"AI attack waves should enter through CommandAttackUnits/command buffer; bridge/waves = {report.WaveBridgeCommands}/{report.WavesLaunched}.");
        }

        if (report.LeftAttackCommands < 1 || report.WaveBridgeCommands < 1)
        {
            failures.Add($"both sides should submit attack commands; left/right command deltas {report.LeftAttackCommands}/{report.WaveBridgeCommands}.");
        }

        if (report.LeftToRightDamage <= 0 || report.RightToLeftDamage <= 0)
        {
            failures.Add($"both sides should cause HP damage; left-to-right/right-to-left {report.LeftToRightDamage:0.0}/{report.RightToLeftDamage:0.0}.");
        }

        if (report.LeftFinalUnitCount < 0
            || report.RightFinalUnitCount < 0
            || report.LeftFinalBuildingCount < 0
            || report.RightFinalBuildingCount < 0)
        {
            failures.Add("final unit/building counts should be non-negative for both sides.");
        }

        if (report.TotalAppliedCommands <= report.HarvestBridgeCommands + report.ProductionBridgeCommands + report.WaveBridgeCommands - 1)
        {
            failures.Add($"applied command total should cover observed bridge deltas; total {report.TotalAppliedCommands}, deltas {report.HarvestBridgeCommands}/{report.ProductionBridgeCommands}/{report.WaveBridgeCommands}.");
        }
    }

    private static void AssertBuildCommandProbe(BuildCommandProbeReport report, List<string> failures)
    {
        if (!report.CommandsWereBuildCommands || report.CommandsSubmitted < 2)
        {
            failures.Add("construction probe should submit StartConstructionEntityCommand build commands.");
        }

        if (!report.CompletedBuildingSpecIds.Contains(BuildingDesignIds.PowerPlant) || !report.CompletedBuildingSpecIds.Contains(BuildingDesignIds.GroundTurret))
        {
            failures.Add($"construction probe should complete AI PowerPlant and GroundTurret via ConstructionSystem; completed [{string.Join(", ", report.CompletedBuildingSpecIds)}].");
        }

        if (report.Rejections != 0)
        {
            failures.Add($"construction probe should have no rejected AI build commands; got {report.Rejections}.");
        }

        if (report.StateHash == 0)
        {
            failures.Add("construction probe should produce a deterministic non-zero state hash.");
        }
    }
}
