static partial class Program
{
    private static void AssertSelectionVfxFogAndWeapons()
    {
        var tankDescriptor = RuntimeDescriptorFor(UnitDesignIds.GenericLightTank);
        var infantryDescriptor = RuntimeDescriptorFor(UnitDesignIds.GenericInfantry);
        var harvesterDescriptor = RuntimeDescriptorFor(UnitDesignIds.GenericHarvester);
        var aircraftDescriptor = UnitDesignDefinitionCatalog.ForDesign("cat.scout_aircraft");
        var unitDesignRuntimeDefinitions = UnitDesignDefinitionCatalog.RuntimeDescriptors.Values.ToArray();

        if (tankDescriptor.AttackRange < 300 || infantryDescriptor.AttackRange < 180 || harvesterDescriptor.AttackRange < 120)
        {
            throw new InvalidOperationException("combat range rebalance should make default unit ranges explicit and longer");
        }

        var boxSelectionState = EmptyState();
        var boxTank = Unit(1, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(500, 500), UnitStance.Hold);
        var boxHarvester = Unit(2, UnitDesignIds.GenericHarvester, Owner.Player, new Vector2(548, 500), UnitStance.Hold);
        boxSelectionState.Units.AddRange([boxTank, boxHarvester]);
        var boxSelected = boxSelectionState.SelectRect(new Rect2(400, 420, 300, 170), additive: false);
        if (boxSelected != 1 || !boxTank.Selected || boxHarvester.Selected)
        {
            throw new InvalidOperationException("broad default box selection should ignore harvesters while selecting combat units");
        }

        boxHarvester.Selected = false;
        boxTank.Selected = false;
        var focusedMixedSelected = boxSelectionState.SelectRect(new Rect2(460, 460, 140, 100), additive: false);
        if (focusedMixedSelected != 2 || !boxTank.Selected || !boxHarvester.Selected)
        {
            throw new InvalidOperationException("focused mixed box selection should include an intentionally framed harvester");
        }

        boxHarvester.Selected = false;
        boxTank.Selected = false;
        var harvesterOnlySelected = boxSelectionState.SelectRect(new Rect2(530, 480, 45, 45), additive: false);
        if (harvesterOnlySelected != 1 || !boxHarvester.Selected || boxTank.Selected)
        {
            throw new InvalidOperationException("box selection should select harvesters when the box contains only harvesters");
        }

        var preciseHarvesterState = EmptyState();
        var preciseEscort = Unit(1, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(522, 500), UnitStance.Hold);
        var preciseHarvester = Unit(2, UnitDesignIds.GenericHarvester, Owner.Player, new Vector2(560, 500), UnitStance.Hold);
        preciseHarvesterState.Units.AddRange([preciseEscort, preciseHarvester]);
        var preciseHarvesterSelected = preciseHarvesterState.SelectRect(new Rect2(520, 480, 70, 50), additive: false);
        if (preciseHarvesterSelected != 2 || !preciseHarvester.Selected || !preciseEscort.Selected)
        {
            throw new InvalidOperationException("small focused box selection should keep an intentionally framed harvester in mixed selection");
        }

        var harvesterMajorityState = EmptyState();
        var majorityEscort = Unit(1, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(520, 500), UnitStance.Hold);
        var majorityHarvesterA = Unit(2, UnitDesignIds.GenericHarvester, Owner.Player, new Vector2(560, 500), UnitStance.Hold);
        var majorityHarvesterB = Unit(3, UnitDesignIds.GenericHarvester, Owner.Player, new Vector2(590, 530), UnitStance.Hold);
        harvesterMajorityState.Units.AddRange([majorityEscort, majorityHarvesterA, majorityHarvesterB]);
        var majoritySelected = harvesterMajorityState.SelectRect(new Rect2(500, 470, 120, 90), additive: false);
        if (majoritySelected != 3 || !majorityHarvesterA.Selected || !majorityHarvesterB.Selected || !majorityEscort.Selected)
        {
            throw new InvalidOperationException("box selection should keep harvesters when the selected cluster is mostly economic units");
        }

        var harvesterRadiusState = EmptyState();
        var radiusHarvester = Unit(1, UnitDesignIds.GenericHarvester, Owner.Player, new Vector2(600, 500), UnitStance.Hold);
        harvesterRadiusState.Units.Add(radiusHarvester);
        var radiusSelected = harvesterRadiusState.SelectRect(new Rect2(580, 470, 12, 60), additive: false);
        if (radiusSelected != 1 || !radiusHarvester.Selected)
        {
            throw new InvalidOperationException("box selection should use unit footprint overlap so large harvesters can be framed by their body");
        }

        var singleSelected = boxSelectionState.SelectSingleAt(boxHarvester.Position, additive: false, pickPadding: 8);
        if (singleSelected != 1 || !boxHarvester.Selected || boxTank.Selected)
        {
            throw new InvalidOperationException("single-click selection should still allow selecting a harvester");
        }

        var sameKindState = EmptyState();
        var doubleClickTank = Unit(1, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(500, 500), UnitStance.Hold);
        var visibleTank = Unit(2, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(640, 520), UnitStance.Hold);
        var offscreenTank = Unit(3, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(1400, 520), UnitStance.Hold);
        var sameKindHarvester = Unit(4, UnitDesignIds.GenericHarvester, Owner.Player, new Vector2(540, 540), UnitStance.Hold);
        var enemyTank = Unit(5, UnitDesignIds.GenericLightTank, Owner.Enemy, new Vector2(560, 520), UnitStance.Hold);
        sameKindState.Units.AddRange([doubleClickTank, visibleTank, offscreenTank, sameKindHarvester, enemyTank]);
        var sameKindSelected = sameKindState.SelectSameUnitsAt(
            doubleClickTank.Position,
            new Rect2(400, 420, 360, 220),
            additive: false,
            pickPadding: 8);
        if (sameKindSelected != 2
            || !doubleClickTank.Selected
            || !visibleTank.Selected
            || offscreenTank.Selected
            || sameKindHarvester.Selected
            || enemyTank.Selected)
        {
            throw new InvalidOperationException("double-click selection should select same-kind visible player units only");
        }

        var sameHarvesterSelected = sameKindState.SelectSameUnitsAt(
            sameKindHarvester.Position,
            new Rect2(400, 420, 360, 220),
            additive: false,
            pickPadding: 8);
        if (sameHarvesterSelected != 1 || !sameKindHarvester.Selected || doubleClickTank.Selected || visibleTank.Selected)
        {
            throw new InvalidOperationException("double-click selection should explicitly allow selecting visible same-kind harvesters");
        }

        if (!Enum.GetValues<AmmoKind>().All(kind => GameState.AmmoDefinitions.ContainsKey(kind)))
        {
            throw new InvalidOperationException("all default ammo types should have data definitions");
        }

        if (GameState.WeaponDefinitions.Values.Any(definition => definition.Hooks == SpecialAttackHook.None))
        {
            throw new InvalidOperationException("weapon definitions should expose special attack hook extension points");
        }

        var lightDeathStyle = DeathVfxMath.StyleFor(UnitWeightClass.Light, MovementDomain.Land, AmmoKind.NeedleDart, 0);
        var heavyRocketDeathStyle = DeathVfxMath.StyleFor(UnitWeightClass.Heavy, MovementDomain.Land, AmmoKind.SeekerRocket, 90);
        var empDeathStyle = DeathVfxMath.StyleFor(UnitWeightClass.Medium, MovementDomain.Land, AmmoKind.ElectromagneticLance, 12);
        if (heavyRocketDeathStyle.FragmentCount <= lightDeathStyle.FragmentCount
            || heavyRocketDeathStyle.SmokeCount <= lightDeathStyle.SmokeCount
            || heavyRocketDeathStyle.BurstScale <= lightDeathStyle.BurstScale
            || heavyRocketDeathStyle.ScorchScale <= lightDeathStyle.ScorchScale
            || heavyRocketDeathStyle.ScorchAlpha <= lightDeathStyle.ScorchAlpha
            || !heavyRocketDeathStyle.EmitsEmbers)
        {
            throw new InvalidOperationException("death VFX should scale up burst, smoke, and fading scorch for heavy overkilled rocket/cannon kills");
        }

        if (!empDeathStyle.EmitsEmpDissolve || empDeathStyle.EmitsEmbers)
        {
            throw new InvalidOperationException("death VFX should expose EMP/ion dissolve hooks separately from ember debris");
        }

        var lightNeedleImpactStyle = ImpactVfxMath.StyleFor(UnitWeightClass.Light, MovementDomain.Land, AmmoKind.NeedleDart, 12);
        var heavyRocketImpactStyle = ImpactVfxMath.StyleFor(UnitWeightClass.Heavy, MovementDomain.Land, AmmoKind.SeekerRocket, 90);
        var airIonImpactStyle = ImpactVfxMath.StyleFor(UnitWeightClass.Medium, MovementDomain.Air, AmmoKind.IonBeam, 34);
        if (heavyRocketImpactStyle.Expansion <= lightNeedleImpactStyle.Expansion
            || heavyRocketImpactStyle.LineWidth <= lightNeedleImpactStyle.LineWidth
            || heavyRocketImpactStyle.SparkCount <= lightNeedleImpactStyle.SparkCount
            || !heavyRocketImpactStyle.EmitsEmbers)
        {
            throw new InvalidOperationException("impact VFX should scale up for heavy rocket/cannon hits");
        }

        if (airIonImpactStyle.SparkScale <= lightNeedleImpactStyle.SparkScale
            || !airIonImpactStyle.EmitsEmpDissolve
            || airIonImpactStyle.EmitsEmbers)
        {
            throw new InvalidOperationException("impact VFX should vary by movement domain and expose ion/EMP dissolve separately");
        }

        if (!GameState.WeaponDefinitions.Values.Any(definition => definition.MountKind == WeaponMountKind.StaticTurret)
            || !GameState.WeaponDefinitions.Values.Any(definition => definition.MountKind == WeaponMountKind.MobileTurret)
            || !GameState.WeaponDefinitions.Values.Any(definition => definition.MountKind == WeaponMountKind.FixedForward))
        {
            throw new InvalidOperationException("weapon definitions should cover static, mobile turret, and fixed-forward mounts");
        }

        if (BuildSpecCatalog.For(BuildingDesignIds.Headquarters).WeaponKind is null)
        {
            throw new InvalidOperationException("headquarters should expose a static defensive weapon source");
        }

        if (GameState.AmmoDefinitions[AmmoKind.SeekerRocket].Behavior != ProjectileBehavior.Tracking)
        {
            throw new InvalidOperationException("seeker rocket should be represented as tracking ammunition");
        }

        if (GameState.AmmoDefinitions[AmmoKind.NeedleDart].HitRule != HitRule.Guaranteed
            || GameState.AmmoDefinitions[AmmoKind.BallisticCannon].HitRule != HitRule.BallisticDeviation
            || GameState.AmmoDefinitions[AmmoKind.ElectromagneticLance].HitRule != HitRule.Guaranteed
            || GameState.AmmoDefinitions[AmmoKind.IonBeam].HitRule != HitRule.Guaranteed
            || GameState.AmmoDefinitions[AmmoKind.SeekerRocket].HitRule != HitRule.Guaranteed)
        {
            throw new InvalidOperationException("default ammunition should expose deterministic hit rule data");
        }

        if (GameState.AmmoDefinitions.Values.Any(ammo => ammo.Behavior == ProjectileBehavior.Beam && (ammo.Speed != 0 || ammo.BeamDuration <= 0))
            || GameState.AmmoDefinitions.Values.Any(ammo => ammo.Behavior != ProjectileBehavior.Beam && ammo.Speed <= 0))
        {
            throw new InvalidOperationException("ammo behavior data should distinguish beams from moving projectiles");
        }

        if (TerrainPassability.AllowedLayers(MovementDomain.Land) != (TerrainLayer.Ground | TerrainLayer.Coast)
            || TerrainPassability.AllowedLayers(MovementDomain.Naval) != (TerrainLayer.Water | TerrainLayer.Coast)
            || TerrainPassability.AllowedLayers(MovementDomain.Air) != (TerrainLayer.Ground | TerrainLayer.Water | TerrainLayer.Coast | TerrainLayer.Air)
            || TerrainPassability.AllowedLayers(MovementDomain.Amphibious) != (TerrainLayer.Ground | TerrainLayer.Water | TerrainLayer.Coast)
            || TerrainPassability.IgnoresBuildingBlockers(MovementDomain.Land)
            || !TerrainPassability.IgnoresBuildingBlockers(MovementDomain.Air))
        {
            throw new InvalidOperationException("terrain passability should define stable allowed layers and blocker behavior per movement domain");
        }

        if (GameState.EffectiveDamageAgainst(AmmoKind.ElectromagneticLance, harvesterDescriptor)
            <= GameState.EffectiveDamageAgainst(AmmoKind.ElectromagneticLance, infantryDescriptor))
        {
            throw new InvalidOperationException("electromagnetic lance should favor heavy armored targets over light targets");
        }

        if (GameState.EffectiveDamageAgainst(AmmoKind.IonBeam, infantryDescriptor)
            <= GameState.EffectiveDamageAgainst(AmmoKind.IonBeam, tankDescriptor))
        {
            throw new InvalidOperationException("ion beam should favor light targets over medium armored targets");
        }

        if (unitDesignRuntimeDefinitions.Any(definition =>
            definition.ArmorTag is not (ArmorTag.Infantry or ArmorTag.Vehicle or ArmorTag.Aircraft))
            || unitDesignRuntimeDefinitions.Any(definition => definition.MovementDomain == MovementDomain.Air && definition.ArmorTag != ArmorTag.Aircraft))
        {
            throw new InvalidOperationException("UnitDesign runtime definitions should expose infantry, vehicle, or aircraft armor tags matching movement domains without legacy GameState unit definitions");
        }

        if (BuildSpecCatalog.Definitions.Values.Any(spec => spec.ArmorTag != ArmorTag.Structure))
        {
            throw new InvalidOperationException("BuildSpec building definitions should expose structure armor tags");
        }

        var fog = new FogOfWarMap(100);
        var firstScoutPosition = new Vector2(160, 160);
        var secondScoutPosition = new Vector2(820, 820);
        fog.Update(new Vector2(1000, 1000), [(firstScoutPosition, 160f)]);
        if (!fog.IsVisible(firstScoutPosition) || !fog.IsExplored(firstScoutPosition))
        {
            throw new InvalidOperationException("fog of war should mark player sight cells visible and explored");
        }

        if (fog.IsVisible(secondScoutPosition) || fog.IsExplored(secondScoutPosition))
        {
            throw new InvalidOperationException("fog of war should keep distant terrain concealed before scouting");
        }

        fog.Update(new Vector2(1000, 1000), []);
        if (fog.IsVisible(firstScoutPosition) || !fog.IsExplored(firstScoutPosition))
        {
            throw new InvalidOperationException("fog of war should clear current visibility while retaining explored memory");
        }

        fog.Update(new Vector2(1000, 1000), [(secondScoutPosition, 130f)]);
        if (!fog.IsVisible(secondScoutPosition) || !fog.IsExplored(firstScoutPosition))
        {
            throw new InvalidOperationException("fog of war should reveal new sight while preserving older explored terrain");
        }

        var defaultFog = new FogOfWarMap();
        defaultFog.Update(new Vector2(3600, 2400), []);
        var defaultFogStats = defaultFog.Stats();
        if (defaultFogStats.Columns != 150 || defaultFogStats.Rows != 100 || defaultFogStats.TotalCells != 15000)
        {
            throw new InvalidOperationException("default fog mask should remain a low-resolution 150x100 tactical mask for the current world size");
        }

        var manyVisionSources = Enumerable.Range(0, 100)
            .Select(index =>
            {
                var x = 120 + index % 20 * 170;
                var y = 120 + index / 20 * 420;
                return (new Vector2(x, y), 180f);
            })
            .ToArray();
        defaultFog.Update(new Vector2(3600, 2400), manyVisionSources);
        var manySourceStats = defaultFog.Stats();
        if (manySourceStats.VisibleCells <= 0
            || manySourceStats.ExploredCells < manySourceStats.VisibleCells
            || manySourceStats.ConcealedCells <= 0)
        {
            throw new InvalidOperationException("fog mask stats should update many vision sources without snapshot allocation or full-map reveal");
        }

        foreach (var ammo in GameState.AmmoDefinitions.Values)
        {
            if (!Enum.GetValues<UnitWeightClass>().All(weight => ammo.DamageProfile.WeightMultipliers.ContainsKey(weight)))
            {
                throw new InvalidOperationException($"{ammo.Kind} should define weight-class damage multipliers");
            }

            if (!Enum.GetValues<ArmorTag>().All(tag => ammo.DamageProfile.ArmorMultipliers.ContainsKey(tag)))
            {
                throw new InvalidOperationException($"{ammo.Kind} should define armor-tag damage multipliers");
            }
        }

        if (GameState.AmmoDefinitions[AmmoKind.SeekerRocket].DamageProfile.Multiplier(UnitWeightClass.Medium, MovementDomain.Air, ArmorTag.Aircraft)
            <= GameState.AmmoDefinitions[AmmoKind.SeekerRocket].DamageProfile.Multiplier(UnitWeightClass.Medium, MovementDomain.Land, ArmorTag.Vehicle))
        {
            throw new InvalidOperationException("seeker rockets should have a domain/tag bonus against aircraft");
        }

        var hqSpec = BuildSpecCatalog.For(BuildingDesignIds.Headquarters);
        var lightRepeaterWeapon = GameState.WeaponDefinitions[WeaponKind.LightRepeater];
        if (!GameState.WeaponCanTarget(lightRepeaterWeapon, aircraftDescriptor)
            || GameState.WeaponTargetPriority(lightRepeaterWeapon, infantryDescriptor) <= GameState.WeaponTargetPriority(lightRepeaterWeapon, aircraftDescriptor)
            || GameState.WeaponTargetPriority(lightRepeaterWeapon, aircraftDescriptor) <= GameState.WeaponTargetPriority(lightRepeaterWeapon, tankDescriptor)
            || GameState.WeaponTargetPriority(lightRepeaterWeapon, hqSpec) >= GameState.WeaponTargetPriority(lightRepeaterWeapon, tankDescriptor))
        {
            throw new InvalidOperationException("light repeater target profile should prefer light ground units, allow weak aircraft engagement, and de-prioritize vehicles/structures");
        }

        if (GameState.WeaponCanTarget(GameState.WeaponDefinitions[WeaponKind.VectorCannon], aircraftDescriptor))
        {
            throw new InvalidOperationException("tank cannon target profile should not allow aircraft engagement in weapon V1");
        }

        if (GameState.EffectiveDamageAgainst(AmmoKind.NeedleDart, hqSpec)
            >= GameState.EffectiveDamageAgainst(AmmoKind.NeedleDart, infantryDescriptor))
        {
            throw new InvalidOperationException("needle dart should be weaker against structure armor than infantry armor");
        }

    }
}
