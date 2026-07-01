static partial class Program
{
    private static void AssertRelationsAndFactionPresentation()
    {
        var tankDescriptor = RuntimeDescriptorFor(UnitDesignIds.GenericLightTank);

        if (FactionRelations.Relation(Owner.Player, FactionId.Dog, Owner.Player, FactionId.Cat) != FactionRelation.Allied
            || !FactionRelations.IsAllied(Owner.Player, FactionId.Dog, Owner.Player, FactionId.Cat)
            || FactionRelations.Relation(Owner.Player, FactionId.Dog, Owner.Enemy, FactionId.Dog) != FactionRelation.Hostile)
        {
            throw new InvalidOperationException("faction relation helpers should separate faction identity from owner/team hostility");
        }

        var ownerOnlyTargetState = new GameState();
        if (!ownerOnlyTargetState.CanOwnerAttack(Owner.Player, Owner.Enemy)
            || ownerOnlyTargetState.CanOwnerAttack(Owner.Player, Owner.Player))
        {
            throw new InvalidOperationException("owner relation table should be the runtime authority for targetable hostility");
        }

        var factionSeedState = new GameState();
        if (factionSeedState.Units.Any(unit => unit.Owner == Owner.Player && unit.FactionId != FactionId.Dog)
            || factionSeedState.Buildings.Any(building => building.Owner == Owner.Player && building.FactionId != FactionId.Dog)
            || factionSeedState.Units.Any(unit => unit.Owner == Owner.Enemy && unit.FactionId != FactionId.Cat)
            || factionSeedState.Buildings.Any(building => building.Owner == Owner.Enemy && building.FactionId != FactionId.Cat))
        {
            throw new InvalidOperationException("seeded skirmish units and buildings should inherit default factions from owner");
        }

        var factionProductionState = new GameState();
        var factionInitialIds = factionProductionState.Units.Select(unit => unit.Id).ToHashSet();
        if (!factionProductionState.EnqueueProduction(ProductionKind.InfantrySquad, Owner.Player, out _))
        {
            throw new InvalidOperationException("player barracks should queue infantry for faction inheritance test");
        }

        var factionProducer = factionProductionState.Buildings.First(building => building.ProductionQueue.Count > 0);
        var factionQueued = factionProducer.ProductionQueue[0];
        if (factionQueued.FactionId != factionProducer.FactionId || factionQueued.FactionId != FactionId.Dog)
        {
            throw new InvalidOperationException("production queue items should inherit faction identity from their producer");
        }

        Advance(factionProductionState, 6.0f);
        var factionCompleted = factionProductionState.CompletedProduction.LastOrDefault();
        var factionProducedUnit = factionProductionState.Units.FirstOrDefault(unit => !factionInitialIds.Contains(unit.Id));
        if (factionCompleted is null
            || factionCompleted.FactionId != FactionId.Dog
            || factionProducedUnit is null
            || factionProducedUnit.FactionId != FactionId.Dog)
        {
            throw new InvalidOperationException("completed production and spawned units should preserve the queued faction identity");
        }

        var relationCombatState = EmptyState();
        var relationAttacker = Unit(1, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(500, 500), UnitStance.Hold, FactionId.Dog);
        var sameOwnerCatAlly = Unit(2, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(680, 500), UnitStance.Hold, FactionId.Cat);
        var enemyDogHostile = Unit(3, UnitDesignIds.GenericLightTank, Owner.Enemy, new Vector2(760, 500), UnitStance.Hold, FactionId.Dog);
        relationCombatState.Units.AddRange([relationAttacker, sameOwnerCatAlly, enemyDogHostile]);
        relationCombatState.SelectUnitsByIds([relationAttacker.Id]);
        relationCombatState.CommandAttackSelected(sameOwnerCatAlly);
        if (relationAttacker.AttackTargetId is not null)
        {
            throw new InvalidOperationException("manual attack commands should not target allied same-owner units even when faction differs");
        }

        relationCombatState.CommandAttackSelected(enemyDogHostile);
        if (relationAttacker.AttackTargetId != enemyDogHostile.Id)
        {
            throw new InvalidOperationException("manual attack commands should target hostile units even when faction identity matches");
        }

        var relationAutoState = EmptyState();
        var autoGuard = Unit(1, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(500, 700), UnitStance.Aggressive, FactionId.Dog);
        var nearbyAlly = Unit(2, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(650, 700), UnitStance.Hold, FactionId.Cat);
        var nearbyHostile = Unit(3, UnitDesignIds.GenericLightTank, Owner.Enemy, new Vector2(730, 700), UnitStance.Hold, FactionId.Dog);
        relationAutoState.Units.AddRange([autoGuard, nearbyAlly, nearbyHostile]);
        Advance(relationAutoState, 0.1f);
        if (autoGuard.AttackTargetId != nearbyHostile.Id)
        {
            throw new InvalidOperationException("auto-acquire should use faction relation helpers instead of owner/faction shortcuts");
        }

        var weaponPriorityState = EmptyState();
        var lightFireVehicle = Unit(1, UnitDesignIds.DogPatrolVehicle, Owner.Player, new Vector2(500, 760), UnitStance.Aggressive, FactionId.Dog);
        var closerTank = Unit(2, UnitDesignIds.CatTank, Owner.Enemy, new Vector2(610, 760), UnitStance.Hold, FactionId.Cat);
        var fartherLight = Unit(3, UnitDesignIds.CatBasic, Owner.Enemy, new Vector2(780, 760), UnitStance.Hold, FactionId.Cat);
        weaponPriorityState.Units.AddRange([lightFireVehicle, closerTank, fartherLight]);
        Advance(weaponPriorityState, 0.1f);
        if (lightFireVehicle.AttackTargetId != fartherLight.Id)
        {
            throw new InvalidOperationException("light fast fire vehicles should prioritize light targets over closer vehicles");
        }

        var weaponLegalityState = EmptyState();
        var tankCannotAttackAir = Unit(1, UnitDesignIds.DogGuardTank, Owner.Player, new Vector2(500, 820), UnitStance.Hold, FactionId.Dog);
        var aircraftTarget = Unit(2, UnitDesignIds.CatScoutAircraft, Owner.Enemy, new Vector2(650, 820), UnitStance.Hold, FactionId.Cat);
        weaponLegalityState.Units.AddRange([tankCannotAttackAir, aircraftTarget]);
        weaponLegalityState.SelectUnitsByIds([tankCannotAttackAir.Id]);
        weaponLegalityState.CommandAttackSelected(aircraftTarget);
        if (tankCannotAttackAir.AttackTargetId is not null)
        {
            throw new InvalidOperationException("manual attack commands should ignore targets outside the selected weapon profile");
        }

        var relationBuildingState = EmptyState();
        var turret = Building(1, BuildingDesignIds.Headquarters, Owner.Player, new Vector2(500, 900), FactionId.Dog);
        var alliedRaider = Unit(2, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(660, 900), UnitStance.Hold, FactionId.Cat);
        var hostileRaider = Unit(3, UnitDesignIds.GenericLightTank, Owner.Enemy, new Vector2(710, 900), UnitStance.Hold, FactionId.Dog);
        relationBuildingState.Buildings.Add(turret);
        relationBuildingState.Units.AddRange([alliedRaider, hostileRaider]);
        Advance(relationBuildingState, 0.1f);
        if (turret.AttackTargetId != hostileRaider.Id)
        {
            throw new InvalidOperationException("building auto-targeting should use hostile relation helpers and ignore allied faction variants");
        }

        var relationAiState = EmptyState();
        relationAiState.Units.Add(Unit(1, UnitDesignIds.GenericLightTank, Owner.Enemy, new Vector2(900, 900), UnitStance.Hold, FactionId.Cat));
        relationAiState.Buildings.Add(Building(1, BuildingDesignIds.Headquarters, Owner.Enemy, new Vector2(760, 900), FactionId.Dog));
        var instantWaveProfile = EnemyDifficultyProfile.Normal with
        {
            AttackInitialDelay = 0,
            AttackWaveInterval = 4,
            MinimumWaveUnits = 1,
            MaximumWaveUnits = 1,
            AggressionRadius = float.PositiveInfinity,
        };
        var relationAi = new EnemyAttackWaveAi(instantWaveProfile);
        relationAi.Update(relationAiState, 0.1);
        if (relationAi.WavesLaunched != 0)
        {
            throw new InvalidOperationException("enemy attack AI should not target same-owner allied structures through owner shortcuts");
        }

        relationAiState.Buildings.Add(Building(2, BuildingDesignIds.Headquarters, Owner.Player, new Vector2(520, 900), FactionId.Dog));
        relationAi = new EnemyAttackWaveAi(instantWaveProfile);
        relationAi.Update(relationAiState, 0.1);
        if (relationAi.WavesLaunched != 1 || relationAiState.Units[0].AttackTargetKind != CombatTargetKind.Building || relationAiState.Units[0].AttackTargetId != 2)
        {
            throw new InvalidOperationException("enemy attack AI should acquire targetable hostile structures through relation helpers");
        }

        var dogSelfAccent = FactionVisualPolicy.EntityAccent(Owner.Player, FactionId.Dog, Owner.Player, FactionId.Dog, tankDescriptor.Accent);
        var dogHostileAccent = FactionVisualPolicy.EntityAccent(Owner.Player, FactionId.Dog, Owner.Enemy, FactionId.Dog, tankDescriptor.Accent);
        var dogSelfOverlay = FactionVisualPolicy.RelationOverlay(FactionRelations.Relation(Owner.Player, FactionId.Dog, Owner.Player, FactionId.Dog));
        var dogHostileOverlay = FactionVisualPolicy.RelationOverlay(FactionRelations.Relation(Owner.Player, FactionId.Dog, Owner.Enemy, FactionId.Dog));
        var dogHostilePip = FactionVisualPolicy.MinimapPip(Owner.Player, FactionId.Dog, Owner.Enemy, FactionId.Dog);
        if (dogSelfAccent != dogHostileAccent || dogSelfOverlay == dogHostileOverlay || dogHostilePip == new Color("#ff5d75"))
        {
            throw new InvalidOperationException("faction visual policy should keep body accent independent from relation color while overlays and minimap carry relation state");
        }

        foreach (var faction in FactionCatalog.Definitions.Values)
        {
            var presentation = PresentationCatalog.Faction(faction.Id);
            if (presentation.Glyph == IconGlyph.None
                || presentation.ShortCode != faction.ShortCode
                || presentation.Accent != faction.Accent
                || presentation.HudColor != faction.HudColor)
            {
                throw new InvalidOperationException($"{faction.Id} faction should expose shared presentation glyph, code, and palette data");
            }
        }

        AssertUnitDesignIdCoverage();

        var dogSpec = UnitDesignCatalog.Spec("dog.infantry");
        var dogPresentation = UnitPresentationCatalog.ForSpec(dogSpec);
        var catPresentation = UnitPresentationCatalog.ForDesign("cat.basic");
        if (dogPresentation.SpecId != dogSpec.Id
            || dogPresentation.NameKey != dogSpec.NameKey
            || dogPresentation.RoleKey != dogSpec.RoleKey
            || dogPresentation.ShortCode != dogSpec.ShortCode
            || dogPresentation.Icon != dogSpec.Icon
            || dogPresentation.PortraitMode != "unit"
            || dogPresentation.Accent != SoftOldCityPalette.FactionColor(dogSpec.Faction)
            || !ReferenceEquals(dogPresentation.Art, dogSpec.Art)
            || dogPresentation.RoleGlyph != dogSpec.Art.StatusGlyph
            || catPresentation.SpecId != "cat.basic"
            || catPresentation.Art.Layers.All(layer => layer.ColorRole != ColorRole.Owner))
        {
            throw new InvalidOperationException("UnitSpec presentation bridge should expose UnitDesign metadata and player-color art");
        }

        foreach (var faction in new[] { UnitFactionId.Dog, UnitFactionId.Cat })
        {
            foreach (var kind in Enum.GetValues<ProductionKind>())
            {
                var spec = ProductionKindDesignBridge.SpecFor(faction, kind);
                var production = spec.Production!;
                var productionPresentation = UnitPresentationCatalog.For(faction, kind);
                var unitPresentation = UnitPresentationCatalog.ForSpec(spec);
                if (productionPresentation.OutputDesignId != spec.Id
                || productionPresentation.Icon != unitPresentation.Icon
                || productionPresentation.RoleGlyph != unitPresentation.RoleGlyph
                || productionPresentation.ShortCode != unitPresentation.ShortCode
                || productionPresentation.Category != production.Category
                || production.LaneIndex < 0
                || string.IsNullOrWhiteSpace(production.LaneKey)
                || !GameText.HasTranslation(production.LaneKey, GameLanguage.English))
                {
                    throw new InvalidOperationException($"{faction} {kind} production presentation should be routed through UnitSpec output descriptor and lane metadata");
                }
            }
        }

        foreach (var BuildingSpecId in BuildingDesignIds.All)
        {
            var spec = BuildSpecCatalog.For(BuildingSpecId);
            if (spec.Icon == IconGlyph.None
                || spec.RoleGlyph == IconGlyph.None
                || spec.Footprint.X <= 0
                || spec.Footprint.Y <= 0
                || !GameText.HasTranslation(spec.NameKey, GameLanguage.English))
            {
                throw new InvalidOperationException($"{BuildingSpecId} should expose BuildSpec presentation metadata and build/runtime data");
            }
        }

        var requiredStructureKinds = new[]
        {
            BuildingDesignIds.Headquarters,
            BuildingDesignIds.PowerPlant,
            BuildingDesignIds.Refinery,
            BuildingDesignIds.Barracks,
            BuildingDesignIds.VehicleFactory,
            BuildingDesignIds.Airfield,
            BuildingDesignIds.GroundTurret,
            BuildingDesignIds.AntiAirTurret,
        };
        if (!requiredStructureKinds.All(kind => BuildSpecCatalog.Definitions.ContainsKey(kind)))
        {
            throw new InvalidOperationException("vertical slice structures should include HQ, power, refinery, barracks, factory, and airfield in BuildSpecCatalog");
        }

        var airfieldSpec = BuildSpecCatalog.For(BuildingDesignIds.Airfield);
        if (airfieldSpec.Category != BuildCategory.Air
            || airfieldSpec.Icon != IconGlyph.Air
            || airfieldSpec.RequiredProducer != BuildingDesignIds.Headquarters
            || !airfieldSpec.RequiredBuildings.SetEquals(new HashSet<string> { BuildingDesignIds.Headquarters, BuildingDesignIds.PowerPlant, BuildingDesignIds.VehicleFactory })
            || airfieldSpec.PowerUsed <= 0
            || airfieldSpec.WeaponKind is not null)
        {
            throw new InvalidOperationException("airfield should be a buildable air-tech structure with vehicle-factory prerequisites and no defensive weapon");
        }

        var groundTurretSpec = BuildSpecCatalog.For(BuildingDesignIds.GroundTurret);
        var antiAirTurretSpec = BuildSpecCatalog.For(BuildingDesignIds.AntiAirTurret);
        if (groundTurretSpec.Category != BuildCategory.Defense
            || antiAirTurretSpec.Category != BuildCategory.Defense
            || groundTurretSpec.WeaponKind != WeaponKind.VectorCannon
            || antiAirTurretSpec.WeaponKind != WeaponKind.SkySpear
            || !BuildSpecCatalog.Definitions.ContainsKey(BuildingDesignIds.GroundTurret)
            || !BuildSpecCatalog.Definitions.ContainsKey(BuildingDesignIds.AntiAirTurret))
        {
            throw new InvalidOperationException("BuildSpecCatalog should expose anti-ground and anti-air defense turrets without FactionCatalog AvailableBuildings");
        }

        var turretTargetingState = EmptyState();
        var groundTurret = Building(1, BuildingDesignIds.GroundTurret, Owner.Player, new Vector2(500, 500), FactionId.Dog);
        var antiAirTurret = Building(2, BuildingDesignIds.AntiAirTurret, Owner.Player, new Vector2(500, 650), FactionId.Dog);
        var hostileTank = Unit(3, UnitDesignIds.CatTank, Owner.Enemy, new Vector2(720, 500), UnitStance.Hold, FactionId.Cat);
        var hostileAircraft = Unit(4, UnitDesignIds.CatScoutAircraft, Owner.Enemy, new Vector2(720, 650), UnitStance.Hold, FactionId.Cat);
        turretTargetingState.Buildings.AddRange([groundTurret, antiAirTurret]);
        turretTargetingState.Units.AddRange([hostileTank, hostileAircraft]);
        Advance(turretTargetingState, 0.2f);
        if (groundTurret.AttackTargetId != hostileTank.Id
            || antiAirTurret.AttackTargetId != hostileAircraft.Id
            || turretTargetingState.Projectiles.All(projectile => projectile.SourceKind != CombatSourceKind.Building || projectile.SourceId == antiAirTurret.Id)
            || turretTargetingState.Projectiles.All(projectile => projectile.SourceKind != CombatSourceKind.Building || projectile.SourceId == groundTurret.Id))
        {
            throw new InvalidOperationException("defense turrets should acquire and fire only at their intended ground/air target classes");
        }

        var dogSelfPresentation = PresentationCatalog.Unit(UnitDesignIds.DogGuardTank, Owner.Player, FactionId.Dog, Owner.Player, FactionId.Dog);
        var dogEnemyPresentation = PresentationCatalog.Unit(UnitDesignIds.DogGuardTank, Owner.Enemy, FactionId.Dog, Owner.Player, FactionId.Dog);
        if (dogSelfPresentation.FactionGlyph != dogEnemyPresentation.FactionGlyph
            || dogSelfPresentation.FactionAccent != dogEnemyPresentation.FactionAccent
            || dogSelfPresentation.EntityAccent != dogEnemyPresentation.EntityAccent
            || dogSelfPresentation.OwnershipOverlay == dogEnemyPresentation.OwnershipOverlay
            || dogSelfPresentation.MinimapPip == dogEnemyPresentation.MinimapPip)
        {
            throw new InvalidOperationException("shared entity presentation should keep body art stable while ownership overlays change by relation");
        }
    }

    private static void AssertUnitDesignIdCoverage()
    {
        var requiredDesignIds = new[]
        {
            UnitDesignIds.GenericInfantry,
            UnitDesignIds.GenericLightTank,
            UnitDesignIds.GenericHarvester,
            UnitDesignIds.DogInfantry,
            UnitDesignIds.DogGuardTank,
            UnitDesignIds.DogHarvester,
            UnitDesignIds.CatBasic,
            UnitDesignIds.CatTank,
            UnitDesignIds.CatHarvester,
        };

        foreach (var designId in requiredDesignIds)
        {
            var spec = UnitDesignCatalog.Spec(designId);
            if (!UnitDesignDefinitionCatalog.RuntimeDescriptors.ContainsKey(designId)
                || UnitPresentationCatalog.ForSpec(spec).SpecId != designId)
            {
                throw new InvalidOperationException($"{designId} should be covered directly by UnitSpec runtime and presentation catalogs");
            }
        }
    }
}
