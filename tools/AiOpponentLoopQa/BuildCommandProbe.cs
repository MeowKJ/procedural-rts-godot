using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Tools.AiOpponentLoopQa;

internal static partial class AiOpponentLoopQaProgram
{
    private static BuildCommandProbeReport RunBuildCommandProbe()
    {
        const int ticks = 30 * 24;
        var ai = OwnerId.FromPlayerSlot(PlayerSlotId.Two);
        var world = new EntityWorld(seed: 8844)
        {
            WorldWidth = 3600,
            WorldHeight = 2400,
        };
        world.AddSystem(new ConstructionSystem());
        world.AddSystem(new PowerSystem());
        world.ResourceInventory(ai).Credits = 2400;

        SpawnCompletedBuilding(world, ai, BuildingDesignIds.Headquarters, new Vector2(2800, 1200), MathF.PI);
        var commandBuffer = new EntityCommandBuffer();
        var commands = new EntityCommand[]
        {
            new StartConstructionEntityCommand(ai, [new EntityId(1)], 1, BuildingDesignIds.PowerPlant, new Vector2(2600, 1030), MathF.PI),
            new StartConstructionEntityCommand(ai, [new EntityId(1)], 210, BuildingDesignIds.GroundTurret, new Vector2(2450, 1200), MathF.PI),
        };
        foreach (var command in commands)
        {
            commandBuffer.Enqueue(command);
        }

        var rejected = 0;
        for (var tick = 1; tick <= ticks; tick++)
        {
            world.Step(tick, 1f / 30f, commandBuffer.DrainUpToTick(tick));
            rejected += world.Events.Drain().OfType<ConstructionRejectedEvent>().Count();
        }

        var completed = world.OrderedEntities
            .Where(entity => entity.OwnerId.Value == ai.Value)
            .Where(entity => entity.Components.TryGet<ConstructionComponentState>(out var construction)
                && construction.Phase == ConstructionPhase.Building
                && construction.Progress >= 1)
            .Select(entity => BuildingSpecIdFor(world, entity))
            .Where(kind => kind is not null)
            .Select(kind => kind!)
            .OrderBy(kind => kind)
            .ToArray();
        var hash = world.DeterministicStateHash();
        var credits = world.ResourceInventory(ai).Credits;

        return new BuildCommandProbeReport(
            CommandsSubmitted: commands.Length,
            CommandsWereBuildCommands: commands.All(command => command is StartConstructionEntityCommand && command.Kind == EntityCommandKind.Build),
            CompletedBuildingSpecIds: completed,
            Rejections: rejected,
            RemainingCredits: credits,
            StateHash: hash);
    }

    private static void SpawnCompletedBuilding(EntityWorld world, OwnerId owner, string kind, Vector2 position, float facing)
    {
        var spec = BuildSpecCatalog.For(kind);
        var components = new List<EntityComponentState>
        {
            new ConstructionIdentityComponentState(kind),
            new HealthComponentState(spec.MaxHp, spec.MaxHp),
            new VisionComponentState(spec.SightRange),
            new FootprintComponentState(spec.Footprint, spec.PlacementDomain),
            new ConstructionComponentState(Progress: 1, BuildTime: spec.BuildTime, Cost: spec.Cost, RefundRatio: spec.RefundRatio),
            new PowerComponentState(spec.PowerProvided, spec.PowerUsed, Powered: true),
        };
        if (spec.BuildRadius > 0)
        {
            components.Add(new BuildRadiusComponentState(spec.BuildRadius));
        }

        world.Spawn(spec.ToEntitySpec(), owner, EntityTransform.At(position, facing), components);
    }

    private static string? BuildingSpecIdFor(EntityWorld world, EntityInstance entity)
    {
        if (entity.Components.TryGet<ConstructionIdentityComponentState>(out var identity))
        {
            return identity.Kind;
        }

        return world.TryGetSpec(entity.SpecId, out var spec)
            ? spec.Authoring.BuildingSpecId
            : null;
    }
}
