static partial class Program
{
    private static void Main()
    {
        AssertScopeAndCatalogs();
        AssertUnitDesignCatalogs();
        AssertFactionShapeLanguage();
        AssertEntityWorldAndPalette();
        AssertPerClassSilhouetteRules();
        AssertBuildSpecBuildingRuntime();
        AssertBuildingEntityComponents();
        AssertUnitDesignRosters();
        AssertUnitBattlefieldCommandsAndCombat();
        AssertUnitBattlefieldProduction();
        AssertRuntimeSnapshotsSmartClickAndBuildingWeapons();
        AssertCommandAcknowledgementEvents();
        AssertRelationsAndFactionPresentation();
        AssertProductionPresentationAndBuildOptions();
        AssertTerrainThemesAndSignals();
        AssertSelectionVfxFogAndWeapons();
        AssertPresentationDescriptorsAndLocalization();
        AssertTacticalAudioDedupe();
        AssertLegacyCombatRules();
        AssertLegacyMovementAndAttackTracking();
        AssertLegacyProductionEconomy();
        AssertSkirmishAiAndSetup();
        AssertDifficultyWavesAndOutcomes();
    }

    private static class UnitDesignIds
    {
        public const string GenericInfantry = "generic.infantry";
        public const string GenericLightTank = "generic.light_tank";
        public const string GenericHarvester = "generic.harvester";
        public const string DogInfantry = "dog.infantry";
        public const string DogRocket = "dog.rocket";
        public const string DogEngineer = "dog.engineer";
        public const string DogPatrolVehicle = "dog.patrol_vehicle";
        public const string DogGuardTank = "dog.guard_tank";
        public const string DogHarvester = "dog.harvester";
        public const string DogSkyPatrolAircraft = "dog.sky_patrol_aircraft";
        public const string DogRepairDog = "dog.repair_dog";
        public const string DogShieldTank = "dog.shield_tank";
        public const string DogSiegeArtillery = "dog.siege_artillery";
        public const string DogAssaultTank = "dog.assault_tank";
        public const string CatBasic = "cat.basic";
        public const string CatRocket = "cat.rocket";
        public const string CatEngineer = "cat.engineer";
        public const string CatScoutCar = "cat.scout_car";
        public const string CatTank = "cat.tank";
        public const string CatHarvester = "cat.harvester";
        public const string CatScoutAircraft = "cat.scout_aircraft";
        public const string CatSniper = "cat.sniper";
        public const string CatRepairVehicle = "cat.repair_vehicle";
        public const string CatShieldVehicle = "cat.shield_vehicle";
        public const string CatSpecial = "cat.special";
        public const string CatCrescentArtillery = "cat.crescent_artillery";
    }

    static UnitSpecRuntimeDescriptor RuntimeDescriptorFor(string designId)
    {
        if (UnitDesignDefinitionCatalog.RuntimeDescriptors.TryGetValue(designId, out var descriptor))
        {
            return descriptor;
        }

        throw new InvalidOperationException($"unit design {designId} should have UnitSpec runtime descriptor coverage in CombatBehavior");
    }

    static FactionId DefaultFactionForOwner(Owner owner)
    {
        return owner == Owner.Player ? FactionId.Dog : FactionId.Cat;
    }

    static UnitModel Unit(int id, string designId, Owner owner, Vector2 position, UnitStance stance, FactionId? factionId = null)
    {
        return Unit(id, RuntimeDescriptorFor(designId), owner, position, stance, factionId);
    }

    static UnitModel Unit(int id, UnitSpecRuntimeDescriptor descriptor, Owner owner, Vector2 position, UnitStance stance, FactionId? factionId = null)
    {
        return new UnitModel
        {
            Id = id,
            DesignId = descriptor.DesignId,
            Owner = owner,
            FactionId = factionId ?? DefaultFactionForOwner(owner),
            Position = position,
            AnchorPosition = position,
            Hp = descriptor.MaxHp,
            Stance = stance,
        };
    }

    static BuildingModel Building(int id, string kind, Owner owner, Vector2 position, FactionId? factionId = null)
    {
        return new BuildingModel
        {
            Id = id,
            Kind = kind,
            Owner = owner,
            FactionId = factionId ?? DefaultFactionForOwner(owner),
            Position = position,
            Hp = BuildSpecCatalog.For(kind).MaxHp,
        };
    }

    static EntityInstance? BuildingEntityForTargetId(UnitBattlefield battlefield, int buildingId)
    {
        return battlefield.BuildingEntityIdByTargetId(buildingId) is { } entityId
            && battlefield.EntityWorld.TryGet(entityId, out var entity)
                ? entity
                : null;
    }

    static bool IsHarvesterUnit(UnitModel unit)
    {
        return GameState.IsHarvesterUnit(unit);
    }

    static bool IsHarvesterDesign(string designId)
    {
        var spec = UnitDesignCatalog.Spec(designId);
        return spec.RoleTags.Contains(UnitRoleTag.Economy)
            && spec.Abilities.Any(ability => ability.Kind == AbilityKind.Harvest);
    }

    static IReadOnlyList<string> StartingDesignIds(UnitFactionId faction)
    {
        return UnitDesignRuntimeLoadouts.StartingUnits(faction)
            .Select(spawn => spawn.DesignId)
            .ToArray();
    }

    static IReadOnlySet<string> PlayableDesignIds(UnitFactionId faction)
    {
        return UnitDesignFactionRosterCatalog.For(faction).PlayableDesignIds.ToHashSet(StringComparer.Ordinal);
    }

    static IReadOnlyList<UnitSpec> PlayableUnitSpecs(UnitFactionId faction)
    {
        return UnitDesignFactionRosterCatalog.For(faction).PlayableDesignIds
            .Select(UnitDesignCatalog.Spec)
            .ToArray();
    }

    static bool IsCombatUnit(UnitModel unit)
    {
        return !IsHarvesterUnit(unit);
    }

    static void Advance(GameState state, float seconds)
    {
        for (var elapsed = 0f; elapsed < seconds; elapsed += 0.05f)
        {
            state.Update(0.05);
        }
    }

    static GameState EmptyState()
    {
        var state = new GameState();
        state.Units.Clear();
        state.Buildings.Clear();
        state.ResourceFields.Clear();
        state.Projectiles.Clear();
        state.Beams.Clear();
        return state;
    }

    static Vector2 FinalMoveDestination(UnitModel unit)
    {
        return unit.Path.Count == 0
            ? unit.MoveTarget ?? unit.Position
            : unit.Path.Last();
    }

    static float ColorDistance(Color a, Color b)
    {
        var dr = a.R - b.R;
        var dg = a.G - b.G;
        var db = a.B - b.B;
        return MathF.Sqrt(dr * dr + dg * dg + db * db);
    }

    private static string[] ExpectedDogPlayableDesignIds() =>
    [
        "dog.engineer",
        "dog.infantry",
        "dog.rocket",
        "dog.guard_tank",
        "dog.patrol_vehicle",
        "dog.harvester",
        "dog.sky_patrol_aircraft",
        "dog.repair_dog",
        "dog.assault_tank",
        "dog.shield_tank",
        "dog.siege_artillery",
    ];

    private static string[] ExpectedCatPlayableDesignIds() =>
    [
        "cat.scout_aircraft",
        "cat.basic",
        "cat.rocket",
        "cat.engineer",
        "cat.scout_car",
        "cat.tank",
        "cat.harvester",
        "cat.sniper",
        "cat.repair_vehicle",
        "cat.shield_vehicle",
        "cat.special",
        "cat.crescent_artillery",
    ];

    private static string[] RequiredDogUnitDesignIds() =>
    [
        "dog.infantry",
        "dog.rocket",
        "dog.engineer",
        "dog.patrol_vehicle",
        "dog.guard_tank",
        "dog.harvester",
    ];

    static int ManualEnemyCombatOrders(GameState state)
    {
        return state.Units.Count(unit =>
            unit.Owner == Owner.Enemy
            && IsCombatUnit(unit)
            && unit.AttackTargetIsManual
            && unit.AttackTargetId is not null);
    }
}
