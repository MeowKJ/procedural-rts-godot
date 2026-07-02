// SimReplay: acceptance gate for the deterministic sim core (docs/Refactor99Plan.md).
// Each scenario runs twice from the same seed + command log and must produce
// identical state hashes at every checkpoint. Covers Phase 1 (movement) and
// Phase 2 (command vocabulary, combat, seeded damage, deaths).

static partial class Program
{
    const int Seed = 1337;

    static void Main()
    {
        RunReplayPreludeAndMovementScenario();
        AssertCommandGatewayValidationShell();
        RunCombatScenario();
        RunTargetStickinessScenario();
        RunTargetReacquireCooldownScenario();
        RunLastKnownTargetMemoryScenario();
        RunAutonomyRadiiScenario();
        RunPassiveRetaliateScenario();
        RunTargetVisibilityScenario();
        RunTargetThreatPriorityScenario();
        RunSharedAllyThreatScenario();
        RunKitingScenario();
        RunUpgradeProgressionScenario();
        RunVeterancyProgressionScenario();
        RunDerivedRegenerationScenario();
        RunProjectileTrackingScenario();
        RunAuthoredContentScenario();
        RunMapAuthoringScenario();
        RunEntitySharedCorridorScenario();
        RunGroupMoveScenario();
        RunAnchoredGroupAttackSlottingScenario();
        RunGroupAttackScenario();
        RunFiringAnchorScenario();
        RunOutcomeScenario();
    }
}
