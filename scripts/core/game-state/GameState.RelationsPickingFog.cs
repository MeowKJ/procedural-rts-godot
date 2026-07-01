using Godot;

namespace ProceduralRts.Core;

public sealed partial class GameState
{
    public FactionRelation RelationToPlayer(Owner subjectOwner, FactionId subjectFaction)
    {
        return FactionRelations.Relation(Owner.Player, MatchConfig.PlayerFaction, subjectOwner, subjectFaction);
    }

    public bool IsAlliedWithPlayer(UnitModel unit)
    {
        return IsOwnerAllied(Owner.Player, unit.Owner);
    }

    public bool IsAlliedWithPlayer(BuildingModel building)
    {
        return IsOwnerAllied(Owner.Player, building.Owner);
    }

    public bool IsHostileToPlayer(UnitModel unit)
    {
        return CanOwnerAttack(Owner.Player, unit.Owner);
    }

    public bool IsHostileToPlayer(BuildingModel building)
    {
        return CanOwnerAttack(Owner.Player, building.Owner);
    }

    public bool IsTargetableHostile(Owner viewerOwner, UnitModel subject)
    {
        return CanOwnerAttack(viewerOwner, subject.Owner);
    }

    public bool IsTargetableHostile(Owner viewerOwner, BuildingModel subject)
    {
        return CanOwnerAttack(viewerOwner, subject.Owner);
    }

    public PlayerRelation OwnerRelation(Owner viewerOwner, Owner subjectOwner)
    {
        return OwnerRelations.Relation(OwnerToRelationId(viewerOwner), OwnerToRelationId(subjectOwner));
    }

    public bool CanOwnerAttack(Owner attackerOwner, Owner targetOwner)
    {
        return OwnerRelations.CanAttack(OwnerToRelationId(attackerOwner), OwnerToRelationId(targetOwner));
    }

    private bool IsOwnerAllied(Owner viewerOwner, Owner subjectOwner)
    {
        return OwnerRelation(viewerOwner, subjectOwner) is PlayerRelation.Self or PlayerRelation.Allied;
    }

    private IReadOnlyList<PlacementBuildAnchor> BuildingBuildAnchors(Owner owner)
    {
        return Buildings
            .Where(building => building.Owner == owner
                && building.Hp > 0
                && building.BuildProgress >= 1
                && BuildSpecCatalog.For(building.Kind).BuildRadius > 0)
            .Select(building =>
            {
                var spec = BuildSpecCatalog.For(building.Kind);
                return new PlacementBuildAnchor(building.Position.X, building.Position.Y, spec.BuildRadius, building.Powered);
            })
            .ToList();
    }

    private static OwnerId OwnerToRelationId(Owner owner)
    {
        return owner switch
        {
            Owner.Player => new OwnerId(1),
            Owner.Enemy => new OwnerId(2),
            _ => OwnerId.None,
        };
    }

    public Color VisualAccent(Owner owner, FactionId factionId, Color roleAccent)
    {
        return FactionVisualPolicy.EntityAccent(Owner.Player, MatchConfig.PlayerFaction, owner, factionId, roleAccent);
    }

    public Color RelationOverlay(Owner owner, FactionId factionId)
    {
        return FactionVisualPolicy.RelationOverlay(RelationToPlayer(owner, factionId));
    }

    public UnitModel? PickEnemyUnit(Vector2 worldPoint, Owner attackerOwner, float pickPadding = 8)
    {
        return PickHostileUnit(worldPoint, attackerOwner, pickPadding);
    }

    public UnitModel? PickHostileUnit(Vector2 worldPoint, Owner attackerOwner, float pickPadding = 8)
    {
        return PickUnit(worldPoint, unit => CanOwnerAttack(attackerOwner, unit.Owner), pickPadding);
    }

    public BuildingModel? PickEnemyBuilding(Vector2 worldPoint, Owner attackerOwner, float pickPadding = 8)
    {
        return PickHostileBuilding(worldPoint, attackerOwner, pickPadding);
    }

    public BuildingModel? PickHostileBuilding(Vector2 worldPoint, Owner attackerOwner, float pickPadding = 8)
    {
        return PickBuilding(worldPoint, building => CanOwnerAttack(attackerOwner, building.Owner), pickPadding);
    }

    public BuildingModel? PickAnyBuilding(Vector2 worldPoint, float pickPadding = 8)
    {
        return PickBuilding(worldPoint, (BuildingModel _) => true, pickPadding);
    }

    public ResourceFieldModel? PickResourceField(Vector2 worldPoint, float pickPadding = 8)
    {
        return ResourceFields
            .Where(field => field.Amount > 0)
            .Select(field => new
            {
                Field = field,
                Distance = field.Position.DistanceTo(worldPoint),
                Radius = field.Radius + pickPadding,
            })
            .Where(candidate => candidate.Distance <= candidate.Radius)
            .OrderBy(candidate => candidate.Distance / Mathf.Max(candidate.Radius, 1))
            .Select(candidate => candidate.Field)
            .FirstOrDefault();
    }

    public UnitModel? PickAnyUnit(Vector2 worldPoint, float pickPadding = 8)
    {
        return PickUnit(worldPoint, (UnitModel _) => true, pickPadding);
    }

    public UnitModel? UnitById(int id)
    {
        return Units.FirstOrDefault(unit => unit.Id == id);
    }

    public BuildingModel? BuildingById(int id)
    {
        return Buildings.FirstOrDefault(building => building.Id == id);
    }

    public ResourceFieldModel? ResourceFieldById(int id)
    {
        return ResourceFields.FirstOrDefault(field => field.Id == id);
    }

    public Vector2? CombatTargetPosition(CombatTargetKind targetKind, int targetId)
    {
        return targetKind switch
        {
            CombatTargetKind.Unit => UnitById(targetId)?.Position,
            CombatTargetKind.Building => BuildingById(targetId)?.Position,
            _ => null,
        };
    }

    public IReadOnlyList<GridObstacle> DebugPathObstacles()
    {
        return PathObstacles(MovementDomain.Land);
    }

    public IReadOnlyList<GridTerrain> DebugTerrainCells()
    {
        return TerrainCells();
    }

    public bool IsVisibleToPlayer(Vector2 worldPosition)
    {
        return FogOfWar.IsVisible(worldPosition);
    }

    public bool IsVisibleToPlayer(UnitModel unit)
    {
        return IsAlliedWithPlayer(unit) || FogOfWar.IsVisible(unit.Position);
    }

    public bool IsExploredByPlayer(BuildingModel building)
    {
        var spec = BuildSpecCatalog.For(building.Kind);
        var worldRect = new Rect2(building.Position - spec.Footprint / 2f, spec.Footprint);
        return IsAlliedWithPlayer(building) || FogOfWar.AnyExplored(worldRect);
    }

    public bool IsExploredByPlayer(Vector2 worldPosition)
    {
        return FogOfWar.IsExplored(worldPosition);
    }

    private void UpdateFogOfWar(
        IEnumerable<(Vector2 Position, float SightRange)>? extraUnitSources = null,
        bool includeLegacyUnitSources = true)
    {
        var unitSources = includeLegacyUnitSources
            ? Units
                .Where(unit => IsAlliedWithPlayer(unit) && unit.Hp > 0)
                .Select(unit => (unit.Position, unit.RuntimeDescriptor.SightRange))
            : [];
        if (extraUnitSources is not null)
        {
            unitSources = unitSources.Concat(extraUnitSources);
        }

        var buildingSources = Buildings
            .Where(building => IsAlliedWithPlayer(building) && building.Hp > 0 && building.BuildProgress >= 1)
            .Select(building => (building.Position, BuildSpecCatalog.For(building.Kind).SightRange));
        var signalSources = SignalNodes
            .Where(node => SignalNetworkMath.EmitsNightVision(node, VisualTheme))
            .Select(node => (node.Position, node.NightVisionRadius));

        _fogUpdateStopwatch.Restart();
        FogOfWar.Update(WorldSize, unitSources.Concat(buildingSources).Concat(signalSources));
        _fogUpdateStopwatch.Stop();
        LastFogUpdateMs = _fogUpdateStopwatch.Elapsed.TotalMilliseconds;
    }

    public float CombatTargetRadius(CombatTargetKind targetKind, int targetId)
    {
        return targetKind switch
        {
            CombatTargetKind.Unit => UnitById(targetId) is { } unit ? unit.RuntimeDescriptor.Radius : 0,
            CombatTargetKind.Building => BuildingById(targetId) is { } building ? BuildingRadius(building) : 0,
            _ => 0,
        };
    }

    private UnitModel? PickUnit(Vector2 worldPoint, Owner owner, float pickPadding)
    {
        return PickUnit(worldPoint, candidateOwner => candidateOwner == owner, pickPadding);
    }

    private UnitModel? PickUnit(Vector2 worldPoint, Func<Owner, bool> ownerPredicate, float pickPadding)
    {
        return PickUnit(worldPoint, unit => ownerPredicate(unit.Owner), pickPadding);
    }

    private UnitModel? PickUnit(Vector2 worldPoint, Func<UnitModel, bool> predicate, float pickPadding)
    {
        return Units
            .Where(predicate)
            .Select(unit => new
            {
                Unit = unit,
                Distance = unit.Position.DistanceTo(worldPoint),
                Radius = unit.RuntimeDescriptor.Radius + pickPadding,
            })
            .Where(candidate => candidate.Distance <= candidate.Radius)
            .OrderBy(candidate => candidate.Distance / Mathf.Max(candidate.Radius, 1))
            .Select(candidate => candidate.Unit)
            .FirstOrDefault();
    }

    private BuildingModel? PickBuilding(Vector2 worldPoint, Func<Owner, bool> ownerPredicate, float pickPadding)
    {
        return PickBuilding(worldPoint, building => ownerPredicate(building.Owner), pickPadding);
    }

    private BuildingModel? PickBuilding(Vector2 worldPoint, Func<BuildingModel, bool> predicate, float pickPadding)
    {
        return Buildings
            .Where(predicate)
            .Select(building => new
            {
                Building = building,
                Distance = building.Position.DistanceTo(worldPoint),
                Radius = BuildingRadius(building) + pickPadding,
            })
            .Where(candidate => candidate.Distance <= candidate.Radius)
            .OrderBy(candidate => candidate.Distance / Mathf.Max(candidate.Radius, 1))
            .Select(candidate => candidate.Building)
            .FirstOrDefault();
    }
}
