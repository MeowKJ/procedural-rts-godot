using Godot;

namespace ProceduralRts.Core;

public sealed partial class ConstructionSystem
{
    private static bool TryGetBuildSpec(string kind, out BuildSpec spec)
    {
        return BuildSpecCatalog.Definitions.TryGetValue(kind, out spec!);
    }

    private PlacementResult ValidateConstructionStart(EntityWorld world, StartConstructionEntityCommand command)
    {
        if (!command.Issuer.IsValid)
        {
            return new PlacementResult(command.Position.X, command.Position.Y, false, "placement.invalidIssuer");
        }

        if (!TryGetBuildSpec(command.BuildingSpecId, out var spec))
        {
            return new PlacementResult(command.Position.X, command.Position.Y, false, "placement.unknownBuilding");
        }

        if (command.ReadyTicket.IsValid)
        {
            return ValidateReadyTicketPlacement(world, command, spec);
        }

        var prerequisites = ValidateConstructionPrerequisites(
            world,
            command.Issuer,
            command.Subjects,
            spec,
            command.Position);
        return prerequisites.IsValid
            ? ValidatePlacementArea(world, command.Issuer, command.Position, spec, RequiresBuildAuthority(spec))
            : prerequisites;
    }

    private static PlacementResult ValidateConstructionQueueStart(EntityWorld world, QueueConstructionEntityCommand command)
    {
        if (!command.Issuer.IsValid)
        {
            return new PlacementResult(0, 0, false, "placement.invalidIssuer");
        }

        if (!TryGetBuildSpec(command.BuildingSpecId, out var spec))
        {
            return new PlacementResult(0, 0, false, "placement.unknownBuilding");
        }

        foreach (var required in spec.RequiredBuildings.OrderBy(kind => kind))
        {
            if (!HasCompletedBuilding(world, command.Issuer, required))
            {
                return new PlacementResult(0, 0, false, "placement.missingTech");
            }
        }

        if (spec.RequiredProducer is { } requiredProducer
            && !HasCompletedProducer(world, command.Issuer, command.Subjects, requiredProducer))
        {
            return new PlacementResult(0, 0, false, "placement.missingProducer");
        }

        return new PlacementResult(0, 0, true, string.Empty);
    }

    private PlacementResult ValidateReadyTicketPlacement(
        EntityWorld world,
        StartConstructionEntityCommand command,
        BuildSpec spec)
    {
        if (!TryGetReadyTicket(world, command, spec, out _, out _, out var reason))
        {
            return new PlacementResult(command.Position.X, command.Position.Y, false, reason);
        }

        return ValidatePlacementArea(world, command.Issuer, command.Position, spec, requiresBuildAuthority: true);
    }

    private static PlacementResult ValidateConstructionPrerequisites(
        EntityWorld world,
        OwnerId issuer,
        IReadOnlyList<EntityId> subjects,
        BuildSpec spec,
        Vector2 position)
    {
        foreach (var required in spec.RequiredBuildings.OrderBy(kind => kind))
        {
            if (!HasCompletedBuilding(world, issuer, required))
            {
                return new PlacementResult(position.X, position.Y, false, "placement.missingTech");
            }
        }

        if (spec.RequiredProducer is { } requiredProducer
            && !HasCompletedProducer(world, issuer, subjects, requiredProducer))
        {
            return new PlacementResult(position.X, position.Y, false, "placement.missingProducer");
        }

        return new PlacementResult(position.X, position.Y, true, string.Empty);
    }

    private PlacementResult ValidatePlacementArea(
        EntityWorld world,
        OwnerId issuer,
        Vector2 position,
        BuildSpec spec,
        bool requiresBuildAuthority)
    {
        BuildAnchors(world, issuer, _placementBuildAnchors);
        FootprintObstacles(world, _placementObstacles);
        BuildVisibilitySources(world, issuer, _placementVisibility);

        return PlacementMath.ValidateBuildableArea(
            position.X,
            position.Y,
            spec.Footprint.X,
            spec.Footprint.Y,
            world.WorldWidth,
            world.WorldHeight,
            spec.PlacementDomain,
            _placementBuildAnchors,
            _placementObstacles,
            terrainAt: (x, y) => TerrainLayerAt(world, x, y),
            requiresBuildAuthority: requiresBuildAuthority,
            buildVisibility: _placementVisibility,
            requiresBuildVisibility: true);
    }

    private static bool RequiresBuildAuthority(BuildSpec spec)
    {
        return spec.RequiredProducer is not null || spec.RequiredBuildings.Count > 0;
    }

    private static bool TryGetReadyTicket(
        EntityWorld world,
        StartConstructionEntityCommand command,
        BuildSpec spec,
        out EntityInstance ticket,
        out ConstructionComponentState construction,
        out string reason)
    {
        ticket = null!;
        construction = null!;
        reason = string.Empty;
        if (!command.ReadyTicket.IsValid || !world.TryGet(command.ReadyTicket, out ticket!))
        {
            reason = "placement.invalidReadyTicket";
            return false;
        }

        if (ticket.OwnerId.Value != command.Issuer.Value)
        {
            reason = "placement.readyTicketOwner";
            return false;
        }

        if (BuildingSpecIdFor(world, ticket) != spec.Kind)
        {
            reason = "placement.readyTicketKind";
            return false;
        }

        if (!ticket.Components.TryGet<ConstructionComponentState>(out construction!))
        {
            reason = "placement.invalidReadyTicket";
            return false;
        }

        if (!construction.ReadyToPlace || construction.Progress < 1)
        {
            reason = "placement.notReady";
            return false;
        }

        return true;
    }

    private static bool HasCompletedProducer(
        EntityWorld world,
        OwnerId ownerId,
        IReadOnlyList<EntityId> subjects,
        string requiredProducer)
    {
        foreach (var subject in subjects.OrderBy(id => id.Value))
        {
            if (world.TryGet(subject, out var entity)
                && IsCompletedBuilding(world, entity, ownerId, requiredProducer))
            {
                return true;
            }
        }

        return HasCompletedBuilding(world, ownerId, requiredProducer);
    }

    private static bool HasCompletedBuilding(EntityWorld world, OwnerId ownerId, string kind)
    {
        return world.OrderedEntities.Any(entity => IsCompletedBuilding(world, entity, ownerId, kind));
    }

    private static bool IsCompletedBuilding(EntityWorld world, EntityInstance entity, OwnerId ownerId, string kind)
    {
        if (entity.OwnerId.Value != ownerId.Value
            || !entity.Components.TryGet<ConstructionComponentState>(out var construction)
            || construction.Phase != ConstructionPhase.Building
            || construction.Progress < 1)
        {
            return false;
        }

        if (entity.Components.TryGet<HealthComponentState>(out var health) && health.Hp <= 0)
        {
            return false;
        }

        return BuildingSpecIdFor(world, entity) == kind;
    }

    private static bool IsCompletedAnyBuilding(EntityWorld world, EntityInstance entity)
    {
        if (BuildingSpecIdFor(world, entity) is null)
        {
            return false;
        }

        if (entity.Components.TryGet<ConstructionComponentState>(out var construction)
            && (construction.Phase != ConstructionPhase.Building || construction.Progress < 1))
        {
            return false;
        }

        return !entity.Components.TryGet<HealthComponentState>(out var health) || health.Hp > 0;
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
