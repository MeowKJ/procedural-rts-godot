static partial class Program
{
    static void RunReplayPreludeAndMovementScenario()
    {
        var worldSize = new Vector2(3600, 2400);

        // ---- Scenario 1: movement ----------------------------------------------------
        const int Movers = 30;
        const int MoveTicks = 6000;

        EntityWorld BuildMovement()
        {
            var world = new EntityWorld(seed: 99);
            world.AddSystem(new CommandSystem());
            world.AddSystem(new MovementSystem());

            var spec = new EntitySpec
            {
                Id = "replay.mover",
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Mover", "replay.mover.name", "replay.mover.role", "MVR", IconGlyph.Infantry),
            };

            var rng = new Random(Seed);
            for (var i = 0; i < Movers; i++)
            {
                var start = new Vector2((float)(rng.NextDouble() * worldSize.X), (float)(rng.NextDouble() * worldSize.Y));
                world.Spawn(spec, new OwnerId(1), EntityTransform.At(start), new EntityComponentState[]
                {
                    new MovementComponentState(Velocity: default),
                    new MovementProfileComponentState(MaxSpeed: 140f),
                });
            }

            return world;
        }

        var moveLog = new List<EntityCommand>();
        var moveRng = new Random(Seed);
        for (var tick = 0; tick < MoveTicks; tick++)
        {
            if (moveRng.Next(0, 40) != 0)
            {
                continue;
            }

            moveLog.Add(new MoveEntityCommand(
                new OwnerId(1),
                new[] { new EntityId(moveRng.Next(1, Movers + 1)) },
                tick,
                new Vector2((float)(moveRng.NextDouble() * worldSize.X), (float)(moveRng.NextDouble() * worldSize.Y)),
                MoveCommandMode.Direct));
        }

        AssertSimClockBacklogMetrics();
        AssertSystemTimingMetrics();
        AssertSharedCorridorPathing();
        AssertEntityWorldPathfinding();
        AssertLiveSimSystemPipeline();
        AssertSimInvariants();
        AssertPresentationMetrics();
        AssertSimEventDrainInto();
        AssertResourceSystem();
        AssertResourceRegeneration();
        AssertAutoHarvestNearestResource();
        AssertProductionSystem();
        AssertConstructionSystem();
        AssertConstructionMethodMetadata();
        AssertDogBuildAuthority();
        AssertRestartCaptureConstruction();
        AssertConstructionQueueReadyState();
        AssertConstructionReadyPlacement();
        AssertPlacementGridFootprints();
        AssertConstructionPowerGate();
        AssertConstructionVisibilityGate();
        AssertConstructionCancelRefund();
        AssertConstructionPausedOffline();
        AssertConstructionDestroyedLifecycle();
        AssertResourceRallyProduction();
        AssertFriendlyUnitRallyProduction();
        AssertRepeatProduction();
        AssertM5TurretEntityProjection();
        AssertPowerConsequences();
        AssertSignalNetworkSystem();
        AssertRepairFieldAbility();
        AssertShieldFieldAbility();
        AssertScanAbility();
        AssertDeployAbility();
        AssertLiveAbilityPlayerCommandRouting();
        AssertLiveRosterDeployAbility();
        AssertAbilityCostAndTargetLegality();
        AssertTargetedRepairCommand();
        AssertPatrolCommandCore();
        AssertGuardCommandCore();
        AssertDeterministic("entity-pathfinding", BuildEntityPathfindingWorld, EntityPathfindingCommands(), 700, 100);
        AssertDeterministic("movement", BuildMovement, moveLog, MoveTicks, 500);
    }
}
