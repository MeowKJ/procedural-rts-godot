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

    private BuildingModel AddBuilding(
        string kind,
        Owner owner,
        Vector2 position,
        float facing = 0,
        FactionId? factionId = null,
        int? legacyId = null,
        float? hp = null,
        float buildProgress = 1)
    {
        var spec = BuildSpecCatalog.For(kind);
        var id = legacyId ?? _nextBuildingId++;
        _nextBuildingId = Math.Max(_nextBuildingId, id + 1);
        var building = new BuildingModel
        {
            Id = id,
            Kind = kind,
            Owner = owner,
            FactionId = factionId ?? MatchConfig.FactionForOwner(owner),
            Position = position,
            Facing = facing,
            TurretFacing = facing,
            Hp = hp ?? spec.MaxHp,
            BuildProgress = buildProgress,
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

    private void SeedAuthoredWorld(MapSpec map, EntityWorld world)
    {
        _mapObstacles.Clear();
        foreach (var obstacle in world.MapEnvironment.StaticObstacles)
        {
            _mapObstacles.Add(new PlacementObstacle(
                obstacle.Bounds.X,
                obstacle.Bounds.Y,
                obstacle.Bounds.Width,
                obstacle.Bounds.Height));
        }

        foreach (var start in map.OwnerStarts)
        {
            ResourceInventories[LegacyOwnerFor(start.OwnerId)].Credits = world.ResourceInventory(start.OwnerId).Credits;
        }

        foreach (var source in map.Resources)
        {
            var entity = world.OrderedEntities.FirstOrDefault(candidate =>
                candidate.SpecId == $"map.resource.{source.Id}")
                ?? throw new InvalidOperationException($"Loaded map resource '{source.Id}' is missing its EntityWorld entity.");
            var node = entity.Components.Require<ResourceNodeComponentState>();
            var collision = entity.Components.Require<CollisionComponentState>();
            AddResourceField(entity.Transform.Position, collision.Radius, node.Amount, source.Accent.ToColor());
        }

        foreach (var entity in world.OrderedEntities)
        {
            if (entity.Components.TryGet<BuildingIdentityComponentState>(out var building))
            {
                var spec = BuildSpecCatalog.For(building.Kind);
                var hp = entity.Components.TryGet<HealthComponentState>(out var health) ? health.Hp : spec.MaxHp;
                var progress = entity.Components.TryGet<ConstructionComponentState>(out var construction)
                    ? construction.Progress
                    : 1;
                AddBuilding(
                    building.Kind,
                    LegacyOwnerFor(entity.OwnerId),
                    entity.Transform.Position,
                    entity.Transform.Facing,
                    LegacyFactionFor(building.Faction),
                    building.LegacyBuildingId,
                    hp,
                    progress);
                continue;
            }

            if (!world.TryGetSpec(entity.SpecId, out var entitySpec) || entitySpec.Kind != EntityKind.Unit)
            {
                continue;
            }

            AddUnit(
                entity.SpecId,
                LegacyOwnerFor(entity.OwnerId),
                entity.Transform.Position,
                entity.Transform.Facing,
                map.StartFor(entity.OwnerId).Faction);
        }
    }

    private static Owner LegacyOwnerFor(OwnerId ownerId)
    {
        return ownerId.Value switch
        {
            1 => Owner.Player,
            2 => Owner.Enemy,
            _ => throw new InvalidOperationException($"Playable authored maps support owner ids 1 and 2, not {ownerId.Value}."),
        };
    }

    private static FactionId LegacyFactionFor(UnitFactionId faction)
    {
        return faction switch
        {
            UnitFactionId.Cat => FactionId.Cat,
            UnitFactionId.Corruption => FactionId.Corruption,
            _ => FactionId.Dog,
        };
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

        AddBuilding(BuildingDesignIds.VehicleFactory, Owner.Player, new Vector2(650, 965), 0);

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
