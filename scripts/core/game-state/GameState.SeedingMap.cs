using Godot;

namespace ProceduralRts.Core;

public sealed partial class GameState
{
    private const string GenericLightTankUnitDesignId = "generic.light_tank";
    private const string GenericInfantryUnitDesignId = "generic.infantry";
    private const string GenericHarvesterUnitDesignId = "generic.harvester";

    private UnitModel AddUnit(string designId, Owner owner, Vector2 position, float facing = 0, FactionId? factionId = null)
    {
        var descriptor = UnitDesignDefinitionCatalog.RuntimeDescriptors[designId];
        var unit = new UnitModel
        {
            Id = _nextId++,
            DesignId = designId,
            Owner = owner,
            FactionId = factionId ?? MatchConfig.FactionForOwner(owner),
            Position = position,
            AnchorPosition = position,
            Facing = facing,
            TurretFacing = facing,
            Hp = descriptor.MaxHp,
        };

        Units.Add(unit);
        UnitAdded?.Invoke(unit);
        return unit;
    }

    private BuildingModel AddBuilding(string kind, Owner owner, Vector2 position, float facing = 0, FactionId? factionId = null)
    {
        var spec = BuildSpecCatalog.For(kind);
        var building = new BuildingModel
        {
            Id = _nextBuildingId++,
            Kind = kind,
            Owner = owner,
            FactionId = factionId ?? MatchConfig.FactionForOwner(owner),
            Position = position,
            Facing = facing,
            TurretFacing = facing,
            Hp = spec.MaxHp,
        };

        Buildings.Add(building);
        BuildingAdded?.Invoke(building);
        return building;
    }

    public BuildingModel UpsertRuntimeBuilding(
        UnitBattlefieldBuildingSnapshot target,
        BuildingPresentationProjection? projection = null)
    {
        var owner = target.PlayerSlotId == PlayerSlotId.One ? Owner.Player : Owner.Enemy;
        var faction = target.Faction switch
        {
            UnitFactionId.Cat => FactionId.Cat,
            UnitFactionId.Corruption => FactionId.Corruption,
            _ => FactionId.Dog,
        };
        var building = BuildingById(target.Id);
        if (building is null)
        {
            building = new BuildingModel
            {
                Id = target.Id,
                Kind = target.Kind,
                Owner = owner,
                FactionId = faction,
                Position = target.Position,
                Facing = target.Facing,
                TurretFacing = target.Facing,
                Hp = target.Hp,
                BuildProgress = projection?.BuildProgress ?? 1,
                Powered = projection?.Powered ?? true,
            };
            Buildings.Add(building);
            _nextBuildingId = Math.Max(_nextBuildingId, target.Id + 1);
            return building;
        }

        building.Position = target.Position;
        building.Facing = target.Facing;
        building.Hp = target.Hp;
        building.BuildProgress = projection?.BuildProgress ?? building.BuildProgress;
        building.Powered = projection?.Powered ?? building.Powered;
        return building;
    }

    private ResourceFieldModel AddResourceField(Vector2 position, float radius, int amount, Color accent)
    {
        var field = new ResourceFieldModel
        {
            Id = _nextResourceFieldId++,
            Position = position,
            Radius = radius,
            Amount = amount,
            MaxAmount = amount,
            Accent = accent,
        };

        ResourceFields.Add(field);
        return field;
    }

    private IReadOnlyList<PlacementObstacle> BuildingObstacles()
    {
        return Buildings
            .Where(building => building.Hp > 0)
            .Select(building =>
            {
                var spec = BuildSpecCatalog.For(building.Kind);
                var rect = PlacementMath.RectFromCenter(
                    building.Position.X,
                    building.Position.Y,
                    spec.Footprint.X + 24,
                    spec.Footprint.Y + 24);
                return new PlacementObstacle(rect.X, rect.Y, rect.Width, rect.Height);
            })
            .ToList();
    }

    private IReadOnlyList<GridObstacle> PathObstacles(
        MovementDomain domain,
        int? movingUnitId = null,
        IReadOnlySet<int>? movingUnitIds = null)
    {
        if (TerrainPassability.IgnoresBuildingBlockers(domain))
        {
            return [];
        }

        return _mapObstacles
            .Concat(BuildingObstacles())
            .Concat(CombatAnchorObstacles(movingUnitId, movingUnitIds))
            .Concat(DenseUnitBlobObstacles(movingUnitId, movingUnitIds))
            .SelectMany(obstacle => GridCellsForObstacle(obstacle, PathCellSize))
            .Distinct()
            .ToList();
    }

    private static bool IsMovingPathSubject(UnitModel unit, int? movingUnitId, IReadOnlySet<int>? movingUnitIds)
    {
        return unit.Id == movingUnitId || (movingUnitIds is not null && movingUnitIds.Contains(unit.Id));
    }

    private IEnumerable<PlacementObstacle> CombatAnchorObstacles(int? movingUnitId, IReadOnlySet<int>? movingUnitIds = null)
    {
        return Units
            .Where(unit => unit.Hp > 0 && !IsMovingPathSubject(unit, movingUnitId, movingUnitIds) && unit.MovementState == UnitMovementState.CombatAnchor)
            .Select(unit =>
            {
                var radius = unit.RuntimeDescriptor.Radius + 18;
                return new PlacementObstacle(
                    unit.Position.X - radius,
                    unit.Position.Y - radius,
                    radius * 2,
                    radius * 2);
            });
    }

    private IEnumerable<PlacementObstacle> DenseUnitBlobObstacles(int? movingUnitId, IReadOnlySet<int>? movingUnitIds = null)
    {
        return Units
            .Where(unit => unit.Hp > 0 && !IsMovingPathSubject(unit, movingUnitId, movingUnitIds))
            .Where(unit => !unit.Selected && unit.MoveTarget is null && unit.MovementState is UnitMovementState.Idle or UnitMovementState.HoldingSlot)
            .GroupBy(unit => LocalAvoidanceMath.Cell(unit.Position.X, unit.Position.Y, DynamicBlobCellSize))
            .Where(group => group.Count() >= DynamicBlobMinimumUnits)
            .Select(group =>
            {
                var members = group.ToList();
                var minX = members.Min(unit => unit.Position.X - unit.RuntimeDescriptor.Radius);
                var minY = members.Min(unit => unit.Position.Y - unit.RuntimeDescriptor.Radius);
                var maxX = members.Max(unit => unit.Position.X + unit.RuntimeDescriptor.Radius);
                var maxY = members.Max(unit => unit.Position.Y + unit.RuntimeDescriptor.Radius);
                return new PlacementObstacle(
                    minX - DynamicBlobObstaclePadding,
                    minY - DynamicBlobObstaclePadding,
                    maxX - minX + DynamicBlobObstaclePadding * 2,
                    maxY - minY + DynamicBlobObstaclePadding * 2);
            });
    }

    private IReadOnlyList<GridTerrain> TerrainCells()
    {
        return [];
    }

    private static IEnumerable<GridObstacle> GridCellsForObstacle(PlacementObstacle obstacle, float cellSize)
    {
        var minX = (int)MathF.Floor(obstacle.X / cellSize);
        var minY = (int)MathF.Floor(obstacle.Y / cellSize);
        var maxX = (int)MathF.Floor((obstacle.X + obstacle.Width) / cellSize);
        var maxY = (int)MathF.Floor((obstacle.Y + obstacle.Height) / cellSize);

        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++)
            {
                yield return new GridObstacle(x, y);
            }
        }
    }

    private void Seed()
    {
        var map = SkirmishMapGenerator.Generate(MatchConfig);
        _mapObstacles.Clear();
        _mapObstacles.AddRange(map.Obstacles);

        SeedOwnerLoadout(MatchStartLoadouts.For(Owner.Player, MatchConfig.PlayerFaction, map));

        foreach (var resource in map.Resources)
        {
            AddResourceField(resource.Position, resource.Radius, resource.Amount, resource.Accent);
        }

        SeedOwnerLoadout(MatchStartLoadouts.For(Owner.Enemy, MatchConfig.AiFaction, map));
    }

    private void SeedOwnerLoadout(MatchStartOwnerLoadout loadout)
    {
        foreach (var building in loadout.Buildings)
        {
            AddBuilding(building.Kind, loadout.Owner, building.Position, building.Facing, loadout.Faction);
        }

        foreach (var unit in loadout.Units)
        {
            AddUnit(unit.DesignId, loadout.Owner, unit.Position, unit.Facing, loadout.Faction);
        }
    }

    private void ConfigureDeveloperSandbox()
    {
        SetVisualTheme(WorldVisualTheme.DayCommand, "developer-sandbox", transitionProgress: 1);

        var testLine = new Vector2(980, 940);
        AddUnit(GenericLightTankUnitDesignId, Owner.Player, testLine, 0.05f);
        AddUnit(GenericLightTankUnitDesignId, Owner.Player, testLine + new Vector2(72, 0), 0.05f);
        AddUnit(GenericInfantryUnitDesignId, Owner.Player, testLine + new Vector2(0, 82));
        AddUnit(GenericInfantryUnitDesignId, Owner.Player, testLine + new Vector2(48, 114));
        AddUnit(GenericHarvesterUnitDesignId, Owner.Player, testLine + new Vector2(140, -46), 0.2f);

        AddBuilding(BuildingDesignIds.VehicleFactory, Owner.Player, new Vector2(650, 965), 0.06f);

        AddUnit(GenericLightTankUnitDesignId, Owner.Enemy, new Vector2(1320, 940), Mathf.Pi);
        AddUnit(GenericInfantryUnitDesignId, Owner.Enemy, new Vector2(1376, 1010), Mathf.Pi);
        AddSandboxFactionTestUnits();
        AddResourceField(new Vector2(1040, 1180), 128, 2600, new Color("#8fffe1"));
    }

    private void AddSandboxFactionTestUnits()
    {
        AddSandboxFactionLine(
            Owner.Player,
            FactionId.Dog,
            new Vector2(820, 1340),
            0.04f);

        AddSandboxFactionLine(
            Owner.Player,
            FactionId.Cat,
            new Vector2(820, 1560),
            0.02f);

        AddSandboxFactionLine(
            Owner.Enemy,
            FactionId.Dog,
            new Vector2(1560, 1340),
            Mathf.Pi);

        AddSandboxFactionLine(
            Owner.Enemy,
            FactionId.Cat,
            new Vector2(1560, 1560),
            Mathf.Pi);
    }

    private void AddSandboxFactionLine(Owner owner, FactionId factionId, Vector2 start, float facing)
    {
        var designIds = UnitDesignFactionRosterCatalog.For(ProductionKindDesignBridge.UnitFactionFor(factionId)).PlayableDesignIds;
        const float spacing = 68;
        const int perRow = 6;
        var spawnIndex = 0;
        foreach (var designId in designIds)
        {
            var column = spawnIndex % perRow;
            var row = spawnIndex / perRow;
            var position = start + new Vector2(column * spacing, row * 74);
            AddUnit(designId, owner, position, facing, factionId);
            spawnIndex++;
        }
    }

    private Vector2 SeededResourcePosition(Vector2 basePosition, int salt)
    {
        if (MatchConfig.MapSeed == SkirmishOptions.DefaultMapSeed)
        {
            return basePosition;
        }

        var seed = unchecked((uint)(MatchConfig.MapSeed * 1103515245 + salt * 12345));
        var xNoise = SeedNoise(seed);
        var yNoise = SeedNoise(seed ^ 0x9e3779b9u);
        var offset = new Vector2((xNoise - 0.5f) * 260f, (yNoise - 0.5f) * 220f);
        return new Vector2(
            Mathf.Clamp(basePosition.X + offset.X, 260, WorldSize.X - 260),
            Mathf.Clamp(basePosition.Y + offset.Y, 260, WorldSize.Y - 260));
    }

    private static float SeedNoise(uint seed)
    {
        seed ^= seed >> 16;
        seed *= 0x7feb352d;
        seed ^= seed >> 15;
        seed *= 0x846ca68b;
        seed ^= seed >> 16;
        return (seed & 0xffff) / 65535f;
    }
}
