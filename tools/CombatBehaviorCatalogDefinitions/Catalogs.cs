static partial class Program
{
    private static void AssertScopeAndCatalogs()
    {
        var ownerScope = Enum.GetValues<Owner>();
        if (ownerScope.Length != 2 || !ownerScope.Contains(Owner.Player) || !ownerScope.Contains(Owner.Enemy))
        {
            throw new InvalidOperationException("vertical slice mode should stay one human player versus one computer AI with no extra owner roles");
        }

        var launchModes = Enum.GetValues<LaunchMode>();
        if (launchModes.Length != 2 || !launchModes.Contains(LaunchMode.Skirmish) || !launchModes.Contains(LaunchMode.Sandbox))
        {
            throw new InvalidOperationException("vertical slice mode should expose only skirmish and developer sandbox launch modes");
        }

        if (launchModes.Any(mode => mode.ToString().Contains("Campaign", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("campaign scope should stay paper-only while the playable vertical slice remains skirmish-only");
        }

        var sandboxTimeScale = SandboxTimeScaleMath.DefaultScale;
        sandboxTimeScale = SandboxTimeScaleMath.Adjust(sandboxTimeScale, 1);
        sandboxTimeScale = SandboxTimeScaleMath.Adjust(sandboxTimeScale, 1);
        var sandboxFastDelta = SandboxTimeScaleMath.ScaledGameplayDelta(0.25, LaunchMode.Sandbox, sandboxTimeScale);
        var skirmishUnscaledDelta = SandboxTimeScaleMath.ScaledGameplayDelta(0.25, LaunchMode.Skirmish, sandboxTimeScale);
        var sandboxSlowest = SandboxTimeScaleMath.Adjust(SandboxTimeScaleMath.Adjust(SandboxTimeScaleMath.Adjust(SandboxTimeScaleMath.DefaultScale, -1), -1), -1);
        var sandboxFastest = SandboxTimeScaleMath.Adjust(SandboxTimeScaleMath.Adjust(SandboxTimeScaleMath.Adjust(SandboxTimeScaleMath.DefaultScale, 1), 1), 1);
        if (Math.Abs(sandboxTimeScale - 4f) > 0.001f
            || Math.Abs(sandboxFastDelta - 1.0) > 0.001
            || Math.Abs(skirmishUnscaledDelta - 0.25) > 0.001
            || Math.Abs(sandboxSlowest - 0.25f) > 0.001f
            || Math.Abs(sandboxFastest - 4f) > 0.001f
            || SandboxTimeScaleMath.Format(2f) != "Sandbox time x2")
        {
            throw new InvalidOperationException("developer sandbox time scale controls should adjust gameplay delta only in sandbox mode");
        }

        var multiplayerConfigNames = typeof(SkirmishOptions)
            .GetProperties()
            .Concat(typeof(MatchConfig).GetProperties())
            .Select(property => property.Name)
            .Where(name => name.Contains("PlayerCount", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Multiplayer", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Network", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Peer", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Server", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Client", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (multiplayerConfigNames.Count > 0)
        {
            throw new InvalidOperationException("vertical slice mode config should not expose player-count or multiplayer networking fields");
        }

        var defaultSkirmish = SkirmishOptions.Default.ToMatchConfig();
        if (defaultSkirmish.LaunchMode != LaunchMode.Skirmish
            || defaultSkirmish.FactionForOwner(Owner.Player) != FactionId.Dog
            || defaultSkirmish.FactionForOwner(Owner.Enemy) != FactionId.Cat)
        {
            throw new InvalidOperationException("default skirmish should map one human player faction against one computer AI faction");
        }

        var relationTable = new PlayerRelationTable();
        if (relationTable.Relation(PlayerSlotId.One, PlayerSlotId.One) != PlayerRelation.Self
            || relationTable.Relation(PlayerSlotId.One, PlayerSlotId.Two) != PlayerRelation.Hostile
            || !relationTable.CanAttack(PlayerSlotId.One, PlayerSlotId.Two))
        {
            throw new InvalidOperationException("current battle relation table should support one local player slot against one hostile AI slot");
        }

    }

    private static void AssertUnitDesignCatalogs()
    {
        var footprintTankRuntimeDefinition = UnitDesignDefinitionCatalog.ForDesign("dog.guard_tank");
        var footprintInfantryRuntimeDefinition = UnitDesignDefinitionCatalog.ForDesign("dog.infantry");
        var footprintHarvesterRuntimeDefinition = UnitDesignDefinitionCatalog.ForDesign("dog.harvester");
        if (footprintTankRuntimeDefinition.WeightClass != UnitWeightClass.Medium
            || footprintInfantryRuntimeDefinition.WeightClass != UnitWeightClass.Light
            || footprintHarvesterRuntimeDefinition.WeightClass != UnitWeightClass.Heavy)
        {
            throw new InvalidOperationException("UnitDesign runtime descriptors should expose light, medium, and heavy weight classes without legacy GameState unit definitions");
        }

        var infantryFootprint = FootprintVisualMath.StyleFor(footprintInfantryRuntimeDefinition);
        var tankFootprint = FootprintVisualMath.StyleFor(footprintTankRuntimeDefinition);
        var harvesterFootprint = FootprintVisualMath.StyleFor(footprintHarvesterRuntimeDefinition);
        if (infantryFootprint.MarkKind != FootprintMarkKind.Step
            || tankFootprint.MarkKind != FootprintMarkKind.TwinTread
            || harvesterFootprint.MarkKind != FootprintMarkKind.TrackPlate)
        {
            throw new InvalidOperationException("footprint styles should distinguish light steps, medium paired treads, and heavy compressed track plates");
        }

        if (!(infantryFootprint.Spacing > tankFootprint.Spacing && tankFootprint.Spacing > harvesterFootprint.Spacing)
            || !(harvesterFootprint.Lifetime > tankFootprint.Lifetime && tankFootprint.Lifetime > infantryFootprint.Lifetime))
        {
            throw new InvalidOperationException("footprint spacing and lifetime should communicate unit weight");
        }

        var aircraftFootprint = FootprintVisualMath.StyleFor(footprintTankRuntimeDefinition with { MovementDomain = MovementDomain.Air });
        var navalFootprint = FootprintVisualMath.StyleFor(footprintTankRuntimeDefinition with { MovementDomain = MovementDomain.Naval });
        if (aircraftFootprint.MarkKind != FootprintMarkKind.Contrail || navalFootprint.MarkKind != FootprintMarkKind.Wake)
        {
            throw new InvalidOperationException("footprint styles should reserve contrails for air units and wake ripples for naval units");
        }

        var tankDescriptor = RuntimeDescriptorFor(UnitDesignIds.GenericLightTank);
        var infantryDescriptor = RuntimeDescriptorFor(UnitDesignIds.GenericInfantry);
        var harvesterDescriptor = RuntimeDescriptorFor(UnitDesignIds.GenericHarvester);
        var aircraftDescriptor = UnitDesignDefinitionCatalog.ForDesign("cat.scout_aircraft");

        var unitDesignRuntimeDefinitions = UnitDesignDefinitionCatalog.RuntimeDescriptors.Values.ToArray();
        var unitDesignWorkerDefinitions = UnitDesignDefinitionCatalog.WithRole(UnitRoleTag.Worker).ToArray();
        foreach (var spec in UnitDesignCatalog.Designs.Values.Select(design => design.ToSpec()))
        {
            var descriptor = UnitDesignDefinitionCatalog.ForSpec(spec);
            if (descriptor.Cost != spec.Stats.Cost
                || descriptor.ProductionCategory != spec.Production?.Category
                || descriptor.ProductionDuration != spec.Production?.Duration
                || descriptor.ProducerKind != spec.Production?.ProducerKind
                || descriptor.ProductionLaneIndex != spec.Production?.LaneIndex
                || descriptor.ProductionLaneKey != spec.Production?.LaneKey)
            {
                throw new InvalidOperationException($"{spec.Id} runtime descriptor should mirror UnitSpec production tuning metadata");
            }
        }

        if (!unitDesignRuntimeDefinitions.Any(definition => definition.MovementDomain == MovementDomain.Land)
            || !unitDesignRuntimeDefinitions.Any(definition => definition.MovementDomain == MovementDomain.Air))
        {
            throw new InvalidOperationException("UnitDesign definition catalog should expose runtime definition data from UnitSpec without reading legacy UnitCatalog definitions");
        }

        if (!unitDesignRuntimeDefinitions.Any(definition => definition.WeightClass == UnitWeightClass.Light && definition.ArmorTag == ArmorTag.Infantry)
            || !unitDesignRuntimeDefinitions.Any(definition => definition.MovementDomain == MovementDomain.Land && definition.ArmorTag == ArmorTag.Vehicle && definition.WeightClass is UnitWeightClass.Medium or UnitWeightClass.Heavy)
            || !unitDesignRuntimeDefinitions.Any(definition => definition.MovementDomain == MovementDomain.Air && definition.ArmorTag == ArmorTag.Aircraft)
            || !unitDesignWorkerDefinitions.Any(definition => definition.MovementDomain == MovementDomain.Land)
            || unitDesignRuntimeDefinitions.Any(definition => definition.MovementDomain is MovementDomain.Naval or MovementDomain.Amphibious || definition.ArmorTag == ArmorTag.Ship))
        {
            throw new InvalidOperationException("UnitDesign runtime definitions should include light, tank/vehicle, aircraft, and harvester units while keeping naval/ship units paper-only");
        }

        var dogFaction = FactionCatalog.For(FactionId.Dog);
        var catFaction = FactionCatalog.For(FactionId.Cat);
        if (FactionCatalog.Definitions.Count < 2
            || !FactionCatalog.Definitions.ContainsKey(FactionId.Dog)
            || !FactionCatalog.Definitions.ContainsKey(FactionId.Cat))
        {
            throw new InvalidOperationException("faction catalog should define dog and cat factions");
        }

        if (FactionCatalog.Definitions.ContainsKey(FactionId.Corruption)
            || FactionRelations.Relation(Owner.Player, FactionId.Corruption, Owner.Enemy, FactionId.Corruption) != FactionRelation.Hostile
            || !FactionRelations.IsAllied(Owner.Player, FactionId.Corruption, Owner.Player, FactionId.Dog))
        {
            throw new InvalidOperationException("third faction should be an enum-only locked placeholder; owner relation must still decide hostility");
        }

        if (dogFaction.ShortCode != "DOG"
            || catFaction.ShortCode != "CAT"
            || dogFaction.Accent == catFaction.Accent
            || dogFaction.HudColor == catFaction.HudColor)
        {
            throw new InvalidOperationException("dog and cat factions should expose distinct display codes and palettes");
        }

        foreach (var faction in new[] { dogFaction, catFaction })
        {
            if (!GameText.HasTranslation(faction.DisplayNameKey, GameLanguage.English)
                || !GameText.HasTranslation(faction.DisplayNameKey, GameLanguage.ChineseSimplified))
            {
                throw new InvalidOperationException($"{faction.Id} faction should expose localized display metadata without owning start-loadout gameplay data");
            }
        }

        if (unitDesignRuntimeDefinitions.Any(definition => definition.TechTier is < 1 or > 3))
        {
            throw new InvalidOperationException("UnitDesign runtime definitions should validate the T1-T3 vertical-slice tier range without legacy GameState unit definitions");
        }

        if (!unitDesignWorkerDefinitions.Any(definition => definition.RoleTags.Contains(UnitRoleTag.Economy) && definition.MovementDomain == MovementDomain.Land))
        {
            throw new InvalidOperationException("UnitDesign runtime role queries should cover land harvester/economy workers without legacy GameState unit definition entries");
        }

        if (aircraftDescriptor.MovementDomain != MovementDomain.Air
            || aircraftDescriptor.ArmorTag != ArmorTag.Aircraft
            || aircraftDescriptor.AttackRange <= 0)
        {
            throw new InvalidOperationException("UnitDesign runtime definitions should project aircraft target metadata without legacy GameState unit definitions");
        }
        AssertAircraftPathingDomain();

        var forbiddenSuperUnitTerms = new[] { "Hero", "Super", "Experimental", "Commander", "Ultimate", "T4", "T5" };
        var unitDesignTypeNames = UnitDesignCatalog.Designs.Values.Select(design => design.GetType().Name).ToArray();
        if (unitDesignTypeNames.Any(name => forbiddenSuperUnitTerms.Any(term => name.Contains(term, StringComparison.OrdinalIgnoreCase))))
        {
            throw new InvalidOperationException("vertical slice must not include hero, super, experimental, T4, or T5 units");
        }

        foreach (var faction in new[] { dogFaction, catFaction })
        {
            var unitFaction = ProductionKindDesignBridge.UnitFactionFor(faction.Id);
            var factionTiers = PlayableUnitSpecs(unitFaction)
                .Select(spec => spec.Stats.TechTier)
                .ToHashSet();
            if (!new[] { 1, 2, 3 }.All(factionTiers.Contains))
            {
                throw new InvalidOperationException($"{faction.Id} UnitDesign playable roster should expose T1, T2, and T3 units without T4/T5 content");
            }
        }

        var expectedDogPlayableDesignIds = ExpectedDogPlayableDesignIds();
        var expectedCatPlayableDesignIds = ExpectedCatPlayableDesignIds();
        var dogPlayableDesignIds = UnitDesignFactionRosterCatalog.For(UnitFactionId.Dog).PlayableDesignIds;
        var catPlayableDesignIds = UnitDesignFactionRosterCatalog.For(UnitFactionId.Cat).PlayableDesignIds;
        if (!dogPlayableDesignIds.SequenceEqual(expectedDogPlayableDesignIds)
            || !catPlayableDesignIds.SequenceEqual(expectedCatPlayableDesignIds)
            || expectedDogPlayableDesignIds.Concat(expectedCatPlayableDesignIds).Any(designId =>
                !UnitDesignDefinitionCatalog.RuntimeDescriptors.ContainsKey(designId)
                || UnitPresentationCatalog.ForDesign(designId).Icon == IconGlyph.None))
        {
            throw new InvalidOperationException("dog and cat faction rosters should expose UnitSpec runtime and presentation descriptors for every planned AI B unit");
        }

        if (dogPlayableDesignIds.Intersect(catPlayableDesignIds).Any())
        {
            throw new InvalidOperationException("dog and cat UnitDesign playable rosters should use faction-specific design ids instead of shared owner-only placeholders");
        }

        if (DefaultFactionForOwner(Owner.Player) != FactionId.Dog
            || DefaultFactionForOwner(Owner.Enemy) != FactionId.Cat)
        {
            throw new InvalidOperationException("CombatBehavior fixture default faction mapping should keep player dog and enemy cat without FactionCatalog default owner metadata");
        }

        if (!IsHarvesterDesign(UnitDesignIds.GenericHarvester)
            || !IsHarvesterDesign(UnitDesignIds.DogHarvester)
            || !IsHarvesterDesign(UnitDesignIds.CatHarvester)
            || IsHarvesterDesign(UnitDesignIds.GenericLightTank)
            || IsHarvesterDesign(UnitDesignIds.DogInfantry))
        {
            throw new InvalidOperationException("harvester checks should resolve generic/dog/cat harvesters through UnitSpec role and harvest ability");
        }

        if (!UnitDesignCatalog.Designs.ContainsKey("dog.guard_tank"))
        {
            throw new InvalidOperationException("unit design catalog should discover inherited unit design classes without a central compatibility registry");
        }

        var requiredDogUnitDesignIds = new[]
        {
            "dog.infantry",
            "dog.rocket",
            "dog.engineer",
            "dog.patrol_vehicle",
            "dog.guard_tank",
            "dog.harvester",
        };
        var requiredUnitDesignIds = requiredDogUnitDesignIds
            .Concat(new[]
            {
            "cat.basic",
            "cat.scout_car",
            "cat.tank",
            "cat.harvester",
            })
            .ToArray();
        if (!requiredUnitDesignIds.All(UnitDesignCatalog.Designs.ContainsKey))
        {
            throw new InvalidOperationException("unit design catalog should discover the first inherited dog T1 unit design set");
        }

        if (UnitDesignCatalog.Designs.Values.Select(design => design.ToSpec()).Any(spec => spec.Stats.TechTier is < 1 or > 3))
        {
            throw new InvalidOperationException("UnitDesign specs must stay within the T1-T3 vertical-slice tier range");
        }

        var unitDesignSpecs = UnitDesignCatalog.Designs.Values.Select(design => design.ToSpec()).ToArray();
        if (!unitDesignSpecs.Any(spec => spec.RoleTags.Contains(UnitRoleTag.Infantry))
            || !unitDesignSpecs.Any(spec => spec.RoleTags.Contains(UnitRoleTag.Vehicle))
            || !unitDesignSpecs.Any(spec => spec.RoleTags.Contains(UnitRoleTag.Aircraft) && spec.Movement.Domain == MovementDomain.Air)
            || !unitDesignSpecs.Any(spec => spec.RoleTags.Contains(UnitRoleTag.Economy) && spec.RoleTags.Contains(UnitRoleTag.Worker))
            || unitDesignSpecs.Any(spec => spec.Movement.Domain is MovementDomain.Naval or MovementDomain.Amphibious || spec.Stats.ArmorTag == ArmorTag.Ship))
        {
            throw new InvalidOperationException("UnitDesign classes should cover infantry, vehicle, aircraft, and harvester/economy roles while excluding playable naval units");
        }

        foreach (var design in UnitDesignCatalog.Designs.Values)
        {
            var spec = design.ToSpec();
            var mountIds = spec.Weapons.Select(mount => mount.MountId).ToHashSet();
            var boundMountIds = spec.Art.Layers
                .Where(layer => layer.Binding.Kind == ArtBindingKind.Mount)
                .Select(layer => layer.Binding.Id)
                .ToHashSet();

            if (string.IsNullOrWhiteSpace(spec.Id)
                || spec.Faction is not (UnitFactionId.Dog or UnitFactionId.Cat or UnitFactionId.Corruption)
                || spec.RoleTags.Count == 0
                || spec.Stats.MaxHp <= 0
                || spec.Movement.Speed <= 0
                || spec.Collision.Radius <= 0
                || spec.Weapons.Count == 0
                || spec.Art.Layers.Count == 0
                || spec.Art.Layers.All(layer => layer.ColorRole != ColorRole.Effect)
                || spec.Art.Layers.All(layer => layer.ColorRole != ColorRole.Owner)
                || !boundMountIds.All(mountIds.Contains))
            {
                throw new InvalidOperationException($"unit design '{design.Id}' should produce a complete clean UnitSpec with valid mount-bound art");
            }
        }

        var dogGuardDesign = UnitDesignCatalog.Spec<DogGuardTank>();
        var dogGuardRuntimeDefinition = UnitDesignDefinitionCatalog.ForSpec(dogGuardDesign);
        if (dogGuardDesign.Id != "dog.guard_tank"
            || dogGuardDesign.Faction != UnitFactionId.Dog
            || dogGuardDesign.Weapons.Count == 0
            || dogGuardDesign.Art.Layers.Count == 0
            || dogGuardDesign.Art.Layers.All(layer => layer.ColorRole != ColorRole.Effect)
            || dogGuardDesign.Art.Layers.All(layer => layer.ColorRole != ColorRole.Owner))
        {
            throw new InvalidOperationException("unit designs should define faction metadata, weapon mounts, faction art, and player color sockets");
        }

        if (dogGuardRuntimeDefinition.DesignId != dogGuardDesign.Id
            || dogGuardRuntimeDefinition.Label != dogGuardDesign.Label
            || dogGuardRuntimeDefinition.WeightClass != dogGuardDesign.Stats.WeightClass
            || dogGuardRuntimeDefinition.MovementDomain != dogGuardDesign.Movement.Domain
            || dogGuardRuntimeDefinition.ArmorTag != dogGuardDesign.Stats.ArmorTag
            || dogGuardRuntimeDefinition.WeaponKind != dogGuardDesign.PrimaryWeapon.WeaponKind
            || dogGuardRuntimeDefinition.MaxHp != dogGuardDesign.Stats.MaxHp
            || dogGuardRuntimeDefinition.Radius != dogGuardDesign.Collision.Radius
            || dogGuardRuntimeDefinition.Speed != dogGuardDesign.Movement.Speed
            || dogGuardRuntimeDefinition.SightRange != dogGuardDesign.Stats.SightRange
            || dogGuardRuntimeDefinition.AttackRange != WeaponCatalog.Weapons[dogGuardDesign.PrimaryWeapon.WeaponKind].Range
            || dogGuardRuntimeDefinition.Damage != WeaponCatalog.Ammo[WeaponCatalog.Weapons[dogGuardDesign.PrimaryWeapon.WeaponKind].AmmoKind].BaseDamage
            || dogGuardRuntimeDefinition.TechTier != dogGuardDesign.Stats.TechTier
            || dogGuardRuntimeDefinition.Cost != dogGuardDesign.Stats.Cost
            || dogGuardRuntimeDefinition.ProductionCategory != dogGuardDesign.Production?.Category
            || dogGuardRuntimeDefinition.ProductionDuration != dogGuardDesign.Production?.Duration
            || dogGuardRuntimeDefinition.ProducerKind != dogGuardDesign.Production?.ProducerKind
            || dogGuardRuntimeDefinition.ProductionLaneIndex != dogGuardDesign.Production?.LaneIndex
            || dogGuardRuntimeDefinition.ProductionLaneKey != dogGuardDesign.Production?.LaneKey)
        {
            throw new InvalidOperationException("UnitDesign definition catalog should project UnitSpec runtime stats directly without legacy runtime projections");
        }

        var dogGuardEntitySpec = dogGuardDesign.ToEntitySpec();
        if (dogGuardEntitySpec.Id != dogGuardDesign.Id
            || dogGuardEntitySpec.Kind != EntityKind.Unit
            || dogGuardEntitySpec.Display.Label != dogGuardDesign.Label
            || dogGuardEntitySpec.Authoring.UnitFaction != UnitFactionId.Dog
            || dogGuardEntitySpec.Stats != dogGuardDesign.Stats
            || dogGuardEntitySpec.Movement != dogGuardDesign.Movement
            || dogGuardEntitySpec.Collision != dogGuardDesign.Collision
            || dogGuardEntitySpec.Weapons.Count != dogGuardDesign.Weapons.Count
            || dogGuardEntitySpec.UnitArt != dogGuardDesign.Art
            || !dogGuardEntitySpec.Tags.Contains(UnitRoleTag.Vehicle.ToString()))
        {
            throw new InvalidOperationException("unit specs should convert into entity specs without mixing authoring faction metadata with runtime ownership");
        }
    }

    private static void AssertAircraftPathingDomain()
    {
        var blockers = new[] { new GridObstacle(2, 1) };
        var terrain = new[] { new GridTerrain(1, 1, TerrainLayer.Water) };
        var landPath = PathfindingMath.FindPathWithDebug(
            32,
            96,
            288,
            96,
            320,
            192,
            64,
            blockers,
            MovementDomain.Land,
            terrain);
        var airPath = PathfindingMath.FindPathWithDebug(
            32,
            96,
            288,
            96,
            320,
            192,
            64,
            blockers,
            MovementDomain.Air,
            terrain);

        if (airPath.Path.Count != 1
            || !SamePathPoint(airPath.Path[0], 288, 96)
            || airPath.RawCells.Count != 2)
        {
            throw new InvalidOperationException("air pathfinding should fly directly over terrain and static blockers");
        }

        if (landPath.Path.Count <= 1)
        {
            throw new InvalidOperationException("land pathfinding should route around the same terrain/blocker wall that aircraft ignore");
        }
    }

    private static bool SamePathPoint(PathPoint point, float x, float y)
    {
        var dx = point.X - x;
        var dy = point.Y - y;
        return dx * dx + dy * dy <= 0.001f;
    }
}
