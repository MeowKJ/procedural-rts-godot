using Godot;
using ProceduralRts.Core;

var failures = new List<string>();

ValidateUnitDesignCatalog(failures);
ValidateDamageElementCatalog(failures);
ValidateWeaponAndAmmoCatalogs(failures);
ValidateBuildingCatalog(failures);
ValidateGenericSpawnPath(failures);
ValidateThrowawayAuthoringPath(failures);

if (failures.Count > 0)
{
    Console.Error.WriteLine("ContentAuthoringQa FAILED:");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    System.Environment.Exit(1);
}

Console.WriteLine(
    $"ContentAuthoringQa PASSED: units {UnitDesignCatalog.Designs.Count}, weapons {WeaponCatalog.WeaponDefinitions.Count}, ammo {WeaponCatalog.AmmoDefinitions.Count}, elements {DamageElementCatalog.Definitions.Count}, build specs {BuildSpecCatalog.Definitions.Count}.");

static void ValidateUnitDesignCatalog(List<string> failures)
{
    var concreteTypes = ConcreteTypes<UnitDesign>();
    Require(UnitDesignCatalog.Designs.Count == concreteTypes.Count, "UnitDesignCatalog must discover every concrete UnitDesign.", failures);
    Require(IsSorted(UnitDesignCatalog.Designs.Keys), "UnitDesignCatalog ids must enumerate in deterministic sorted order.", failures);

    foreach (var spec in UnitDesignCatalog.Designs.Values.Select(design => design.ToSpec()))
    {
        var entitySpec = spec.ToEntitySpec();
        Require(entitySpec.Id == spec.Id && entitySpec.Kind == EntityKind.Unit, $"{spec.Id} must project to a unit EntitySpec.", failures);
        ValidateElementDefense(spec.Stats.ElementDefense, $"{spec.Id} stats", failures);
        foreach (var mount in spec.Weapons)
        {
            Require(WeaponCatalog.WeaponDefinitions.ContainsKey(mount.WeaponId), $"{spec.Id} references missing weapon {mount.WeaponId}.", failures);
        }
    }
}

static void ValidateWeaponAndAmmoCatalogs(List<string> failures)
{
    Require(WeaponCatalog.WeaponDefinitions.Count == ConcreteTypes<WeaponDesign>().Count, "WeaponCatalog must discover every concrete WeaponDesign.", failures);
    Require(WeaponCatalog.AmmoDefinitions.Count == ConcreteTypes<AmmoDesign>().Count, "WeaponCatalog must discover every concrete AmmoDesign.", failures);
    Require(Enum.GetValues<WeaponKind>().All(WeaponCatalog.Weapons.ContainsKey), "Every WeaponKind must have a discovered WeaponDesign.", failures);
    Require(Enum.GetValues<AmmoKind>().All(WeaponCatalog.Ammo.ContainsKey), "Every AmmoKind must have a discovered AmmoDesign.", failures);

    foreach (var weapon in WeaponCatalog.WeaponDefinitions.Values)
    {
        Require(WeaponCatalog.AmmoDefinitions.ContainsKey(weapon.AmmoId), $"{weapon.Id} references missing ammo {weapon.AmmoId}.", failures);
        Require(weapon.Range > 0 && weapon.Cooldown > 0, $"{weapon.Id} must have positive range and cooldown.", failures);
        Require(weapon.TargetProfile.AllowedDomains.Count > 0, $"{weapon.Id} must declare target domains.", failures);
        Require(weapon.TargetProfile.AllowedArmorTags.Count > 0, $"{weapon.Id} must declare target armor.", failures);
    }

    foreach (var ammo in WeaponCatalog.AmmoDefinitions.Values)
    {
        Require(ammo.BaseDamage > 0, $"{ammo.Id} must have positive base damage.", failures);
        Require(ammo.DamageProfile.ArmorMultipliers.Count > 0, $"{ammo.Id} must declare armor damage multipliers.", failures);
        Require(DamageElementCatalog.Definitions.ContainsKey(ammo.DamageElementId), $"{ammo.Id} references missing damage element {ammo.DamageElementId}.", failures);
        Require(ammo.CounterRules.Rules.All(rule => rule.Multiplier > 0), $"{ammo.Id} counter rules must use positive multipliers.", failures);
    }
}

static void ValidateDamageElementCatalog(List<string> failures)
{
    Require(DamageElementCatalog.Definitions.Count == DamageElementIds.All.Count, "DamageElementCatalog must define every stable DamageElementIds entry.", failures);
    Require(DamageElementIds.All.SequenceEqual(DamageElementCatalog.Definitions.Keys), "DamageElementIds.All must mirror discovered damage element definitions in deterministic order.", failures);
    foreach (var id in DamageElementIds.All)
    {
        Require(DamageElementCatalog.Definitions.TryGetValue(id, out var definition), $"DamageElementCatalog missing {id}.", failures);
        if (DamageElementCatalog.Definitions.TryGetValue(id, out definition))
        {
            Require(definition.Id == id, $"{id} damage element definition id must match the catalog key.", failures);
            Require(!string.IsNullOrWhiteSpace(definition.Label), $"{id} damage element must have a label.", failures);
            Require(definition.DamageMultiplier > 0, $"{id} damage element must have a positive neutral/default multiplier.", failures);
        }
    }
}

static void ValidateBuildingCatalog(List<string> failures)
{
    var concreteTypes = ConcreteTypes<BuildingDesign>();
    Require(BuildSpecCatalog.Definitions.Count == concreteTypes.Count, "BuildSpecCatalog must discover every concrete BuildingDesign.", failures);
    Require(BuildingDesignIds.All.SequenceEqual(BuildSpecCatalog.Definitions.Keys), "BuildingDesignIds.All must mirror discovered build specs.", failures);

    foreach (var spec in BuildSpecCatalog.Definitions.Values)
    {
        var entitySpec = spec.ToEntitySpec();
        var expectedKind = spec.WeaponKind is null ? EntityKind.Building : EntityKind.Turret;
        Require(entitySpec.Id == spec.EntitySpecId && entitySpec.Kind == expectedKind, $"{spec.Kind} must project to the expected EntityKind.", failures);
        Require(spec.RequiredProducer is null || BuildSpecCatalog.Definitions.ContainsKey(spec.RequiredProducer), $"{spec.Kind} has a missing required producer.", failures);
        ValidateElementDefense(spec.ElementDefense, $"{spec.Kind} build spec", failures);
        foreach (var required in spec.RequiredBuildings)
        {
            Require(BuildSpecCatalog.Definitions.ContainsKey(required), $"{spec.Kind} requires missing building {required}.", failures);
        }

        if (spec.WeaponKind is { } weaponKind)
        {
            Require(WeaponCatalog.Weapons.ContainsKey(weaponKind), $"{spec.Kind} references missing weapon {weaponKind}.", failures);
        }
    }
}

static void ValidateElementDefense(ElementDefenseProfile? profile, string owner, List<string> failures)
{
    if (profile is null)
    {
        return;
    }

    foreach (var pair in profile.ElementMultipliers)
    {
        Require(DamageElementCatalog.Definitions.ContainsKey(pair.Key), $"{owner} references missing damage element {pair.Key}.", failures);
        Require(pair.Value > 0, $"{owner} element defense multiplier for {pair.Key} must be positive.", failures);
    }
}

static void ValidateGenericSpawnPath(List<string> failures)
{
    var world = new EntityWorld(seed: 99);
    SimSystemPipeline.ConfigureLiveGameplay(world, new OwnerId(1));

    var unitSpec = UnitDesignCatalog.Spec("dog.guard_tank");
    var unit = world.SpawnUnit(unitSpec, new OwnerId(1), new Vector2(120, 120));
    var buildSpec = BuildSpecCatalog.For(BuildingDesignIds.GroundTurret);
    var building = world.SpawnBuildingTarget(
        new BuildingEntitySeed(7, buildSpec.Kind, PlayerSlotId.One, UnitFactionId.Dog, new Vector2(260, 120), 0, buildSpec.MaxHp),
        buildSpec);

    world.Step(0, 1f / 30f, []);
    Require(world.TryGet(unit.Id, out _), "Spawned UnitDesign entity must remain in the world after one generic tick.", failures);
    Require(world.TryGet(building.Id, out _), "Spawned BuildSpec entity must remain in the world after one generic tick.", failures);
}

static void ValidateThrowawayAuthoringPath(List<string> failures)
{
    var toolAssembly = typeof(ThrowawayProbeUnitDesign).Assembly;
    var toolUnits = UnitDesignCatalog.DiscoverDesignsFrom(toolAssembly);
    var toolBuildings = BuildSpecCatalog.DiscoverDefinitionsFrom(toolAssembly);
    var toolWeapons = WeaponCatalog.DiscoverWeaponsFrom(toolAssembly);
    var toolAmmo = WeaponCatalog.DiscoverAmmoFrom(toolAssembly);
    Require(toolUnits.ContainsKey("qa.throwaway.probe_unit"), "Tool-local throwaway UnitDesign must be discovered by assembly scan.", failures);
    Require(toolBuildings.ContainsKey("qa.throwaway.probe_building"), "Tool-local throwaway BuildingDesign must be discovered by assembly scan.", failures);
    Require(toolWeapons.ContainsKey(ThrowawayProbeWeaponDesign.WeaponId), "Tool-local throwaway WeaponDesign must be discovered by assembly scan.", failures);
    Require(toolAmmo.ContainsKey(ThrowawayProbeWeaponDesign.AmmoId), "Tool-local throwaway AmmoDesign must be discovered by assembly scan.", failures);
    Require(!UnitDesignCatalog.Designs.ContainsKey("qa.throwaway.probe_unit"), "Throwaway UnitDesign must not pollute the runtime UnitDesignCatalog.", failures);
    Require(!BuildSpecCatalog.Definitions.ContainsKey("qa.throwaway.probe_building"), "Throwaway BuildingDesign must not pollute the runtime BuildSpecCatalog.", failures);
    Require(!WeaponCatalog.WeaponDefinitions.ContainsKey(ThrowawayProbeWeaponDesign.WeaponId), "Throwaway WeaponDesign must not pollute the runtime WeaponCatalog.", failures);
    Require(!WeaponCatalog.AmmoDefinitions.ContainsKey(ThrowawayProbeWeaponDesign.AmmoId), "Throwaway AmmoDesign must not pollute the runtime WeaponCatalog.", failures);

    var unitSpec = toolUnits["qa.throwaway.probe_unit"].ToSpec();
    var buildSpec = toolBuildings["qa.throwaway.probe_building"];
    Require(unitSpec.ToEntitySpec().Kind == EntityKind.Unit, "Throwaway UnitDesign must project through the generic UnitSpec bridge.", failures);
    Require(buildSpec.ToEntitySpec().Kind == EntityKind.Building, "Throwaway BuildingDesign must project through the generic BuildSpec bridge.", failures);
    Require(unitSpec.PrimaryWeapon.WeaponId == ThrowawayProbeWeaponDesign.WeaponId, "Throwaway UnitDesign must mount the tool-local string weapon id.", failures);

    var combatWorld = new EntityWorld(seed: 606) { WorldWidth = 800, WorldHeight = 500 };
    combatWorld.RegisterCombatDefinitions(toolWeapons.Values, toolAmmo.Values);
    SimSystemPipeline.ConfigureLiveGameplay(combatWorld, new OwnerId(1));
    combatWorld.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);
    var attacker = combatWorld.SpawnUnit(unitSpec, new OwnerId(1), new Vector2(200, 220));
    var target = combatWorld.SpawnUnit(unitSpec, new OwnerId(2), new Vector2(310, 220), MathF.PI);
    for (var tick = 1; tick <= 90; tick++)
    {
        combatWorld.Step(tick, 1f / 30f, []);
        combatWorld.Events.Drain();
    }

    Require(combatWorld.TryGet(attacker.Id, out _), "Throwaway authored unit attacker must remain valid after generic combat ticks.", failures);
    Require(combatWorld.TryGet(target.Id, out var damagedTarget)
        && damagedTarget.Components.Require<HealthComponentState>().Hp < unitSpec.Stats.MaxHp,
        "Throwaway authored unit must fight through generic combat systems.", failures);

    var buildWorld = new EntityWorld(seed: 707) { WorldWidth = 800, WorldHeight = 500 };
    buildWorld.AddSystem(new ConstructionSystem());
    var building = buildWorld.Spawn(
        buildSpec.ToEntitySpec(),
        new OwnerId(1),
        EntityTransform.At(new Vector2(420, 240)),
        ThrowawayBuildingComponents(buildSpec));
    for (var tick = 1; tick <= 12; tick++)
    {
        buildWorld.Step(tick, 1f / 30f, []);
        buildWorld.Events.Drain();
    }

    Require(buildWorld.TryGet(building.Id, out var completedBuilding)
        && completedBuilding.Components.Require<ConstructionComponentState>().Progress >= 1,
        "Throwaway authored building must build through the generic ConstructionSystem.", failures);
}

static IEnumerable<EntityComponentState> ThrowawayBuildingComponents(BuildSpec spec)
{
    yield return new ConstructionIdentityComponentState(spec.Kind);
    yield return new HealthComponentState(spec.MaxHp, spec.MaxHp);
    yield return new SelectableComponentState();
    yield return new VisionComponentState(spec.SightRange);
    yield return new CollisionComponentState(MathF.Max(spec.Footprint.X, spec.Footprint.Y) * 0.5f, 8, 100, BlocksMovement: true);
    yield return new FootprintComponentState(spec.Footprint, spec.PlacementDomain);
    yield return new ConstructionComponentState(0, spec.BuildTime, spec.Cost, spec.RefundRatio);
    yield return new PowerComponentState(spec.PowerProvided, spec.PowerUsed, Powered: true);
}

static IReadOnlyList<Type> ConcreteTypes<TBase>()
{
    return typeof(TBase)
        .Assembly
        .GetTypes()
        .Where(type => !type.IsAbstract && typeof(TBase).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) is not null)
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();
}

static bool IsSorted(IEnumerable<string> values)
{
    return values.SequenceEqual(values.OrderBy(value => value, StringComparer.Ordinal));
}

static void Require(bool condition, string message, List<string> failures)
{
    if (!condition)
    {
        failures.Add(message);
    }
}
