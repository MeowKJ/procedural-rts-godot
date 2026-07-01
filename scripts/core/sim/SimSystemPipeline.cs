namespace ProceduralRts.Core;

/// <summary>
/// Shared EntityWorld system wiring. Live gameplay and full-pipeline replays
/// must call this instead of hand-registering the authoritative system order.
/// </summary>
public static class SimSystemPipeline
{
    public static void ConfigureLiveGameplay(EntityWorld world, OwnerId outcomeViewer)
    {
        world.AddSystem(new CommandSystem());
        world.AddSystem(new ConstructionSystem());
        world.AddSystem(new PowerSystem());
        world.AddSystem(new SignalNetworkSystem());
        world.AddSystem(new AbilitySystem());
        world.AddSystem(new RepairSystem());
        world.AddSystem(new RegenerationSystem());
        world.AddSystem(new VisionSystem());
        world.AddSystem(new CombatSystem());
        world.AddSystem(new ProjectileSystem());
        world.AddSystem(new PathfindingSystem());
        world.AddSystem(new MovementSystem());
        world.AddSystem(new SeparationSystem());
        world.AddSystem(new OutcomeSystem(outcomeViewer));
    }
}
