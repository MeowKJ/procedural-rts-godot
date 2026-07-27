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

        if (!AmmoIds.All.All(WeaponCatalog.AmmoDefinitions.ContainsKey))
        {
            throw new InvalidOperationException("all default ammo types should have data definitions");
        }

        if (WeaponCatalog.WeaponDefinitions.Values.Any(definition => definition.Hooks == SpecialAttackHook.None))
        {
            throw new InvalidOperationException("weapon definitions should expose special attack hook extension points");
        }

        var lightDeathStyle = DeathVfxMath.StyleFor(UnitWeightClass.Light, MovementDomain.Land, AmmoIds.NeedleDart, 0);
        var heavyRocketDeathStyle = DeathVfxMath.StyleFor(UnitWeightClass.Heavy, MovementDomain.Land, AmmoIds.SeekerRocket, 90);
        var empDeathStyle = DeathVfxMath.StyleFor(UnitWeightClass.Medium, MovementDomain.Land, AmmoIds.ElectromagneticLance, 12);
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

        var lightNeedleImpactStyle = ImpactVfxMath.StyleFor(UnitWeightClass.Light, MovementDomain.Land, AmmoIds.NeedleDart, 12);
        var heavyRocketImpactStyle = ImpactVfxMath.StyleFor(UnitWeightClass.Heavy, MovementDomain.Land, AmmoIds.SeekerRocket, 90);
        var airIonImpactStyle = ImpactVfxMath.StyleFor(UnitWeightClass.Medium, MovementDomain.Air, AmmoIds.IonBeam, 34);
        if (heavyRocketImpactStyle.Expansion <= lightNeedleImpactStyle.Expansion
            || heavyRocketImpactStyle.LineWidth <= lightNeedleImpactStyle.LineWidth
            || heavyRocketImpactStyle.SparkCount <= lightNeedleImpactStyle.SparkCount
            || heavyRocketImpactStyle.ShakeAmplitude <= lightNeedleImpactStyle.ShakeAmplitude
            || heavyRocketImpactStyle.ShakeRadius <= 0
            || !heavyRocketImpactStyle.EmitsEmbers)
        {
            throw new InvalidOperationException("impact VFX should scale up flash, spark, and optional shake for heavy rocket/cannon hits");
        }

        if (lightNeedleImpactStyle.ShakeAmplitude > 0 || lightNeedleImpactStyle.ShakeRadius > 0)
        {
            throw new InvalidOperationException("needle impact VFX should stay crisp without screen shake");
        }

        if (airIonImpactStyle.SparkScale <= lightNeedleImpactStyle.SparkScale
            || airIonImpactStyle.ShakeAmplitude > 0
            || !airIonImpactStyle.EmitsEmpDissolve
            || airIonImpactStyle.EmitsEmbers)
        {
            throw new InvalidOperationException("impact VFX should vary by movement domain and expose ion/EMP dissolve separately");
        }

        var visibleCombatReadability = CombatReadabilityMath.StyleFor(visibleToPlayer: true, exploredByPlayer: true, activeEffectCount: 24, commandMarkerCount: 0);
        var commandCombatReadability = CombatReadabilityMath.StyleFor(visibleToPlayer: true, exploredByPlayer: true, activeEffectCount: 24, commandMarkerCount: 2);
        var fogCombatReadability = CombatReadabilityMath.StyleFor(visibleToPlayer: false, exploredByPlayer: true, activeEffectCount: 24, commandMarkerCount: 0);
        var loadedCombatReadability = CombatReadabilityMath.StyleFor(visibleToPlayer: true, exploredByPlayer: true, activeEffectCount: 160, commandMarkerCount: 0);
        var hiddenCombatReadability = CombatReadabilityMath.StyleFor(visibleToPlayer: false, exploredByPlayer: false, activeEffectCount: 24, commandMarkerCount: 0);
        if (!visibleCombatReadability.Draw
            || !commandCombatReadability.Draw
            || !fogCombatReadability.Draw
            || hiddenCombatReadability.Draw
            || commandCombatReadability.AlphaScale >= visibleCombatReadability.AlphaScale
            || fogCombatReadability.AlphaScale >= commandCombatReadability.AlphaScale
            || loadedCombatReadability.AlphaScale >= visibleCombatReadability.AlphaScale
            || commandCombatReadability.LineWidthScale >= visibleCombatReadability.LineWidthScale)
        {
            throw new InvalidOperationException("combat readability should keep transient juice below command markers and strongly suppressed by fog/load");
        }

        if (!WeaponCatalog.WeaponDefinitions.Values.Any(definition => definition.MountKind == WeaponMountKind.StaticTurret)
            || !WeaponCatalog.WeaponDefinitions.Values.Any(definition => definition.MountKind == WeaponMountKind.MobileTurret)
            || !WeaponCatalog.WeaponDefinitions.Values.Any(definition => definition.MountKind == WeaponMountKind.FixedForward))
        {
            throw new InvalidOperationException("weapon definitions should cover static, mobile turret, and fixed-forward mounts");
        }

        if (BuildSpecCatalog.For(BuildingDesignIds.Headquarters).WeaponId is null)
        {
            throw new InvalidOperationException("headquarters should expose a static defensive weapon source");
        }

        if (WeaponCatalog.AmmoDefinitions[AmmoIds.SeekerRocket].Behavior != ProjectileBehavior.Tracking)
        {
            throw new InvalidOperationException("seeker rocket should be represented as tracking ammunition");
        }

        if (WeaponCatalog.AmmoDefinitions[AmmoIds.NeedleDart].HitRule != HitRule.Guaranteed
            || WeaponCatalog.AmmoDefinitions[AmmoIds.BallisticCannon].HitRule != HitRule.BallisticDeviation
            || WeaponCatalog.AmmoDefinitions[AmmoIds.ElectromagneticLance].HitRule != HitRule.Guaranteed
            || WeaponCatalog.AmmoDefinitions[AmmoIds.IonBeam].HitRule != HitRule.Guaranteed
            || WeaponCatalog.AmmoDefinitions[AmmoIds.SeekerRocket].HitRule != HitRule.Guaranteed)
        {
            throw new InvalidOperationException("default ammunition should expose deterministic hit rule data");
        }

        if (WeaponCatalog.AmmoDefinitions.Values.Any(ammo => ammo.Behavior == ProjectileBehavior.Beam && (ammo.Speed != 0 || ammo.BeamDuration <= 0))
            || WeaponCatalog.AmmoDefinitions.Values.Any(ammo => ammo.Behavior != ProjectileBehavior.Beam && ammo.Speed <= 0))
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

        if (EffectiveDamageAgainst(AmmoIds.ElectromagneticLance, harvesterDescriptor)
            <= EffectiveDamageAgainst(AmmoIds.ElectromagneticLance, infantryDescriptor))
        {
            throw new InvalidOperationException("electromagnetic lance should favor heavy armored targets over light targets");
        }

        if (EffectiveDamageAgainst(AmmoIds.IonBeam, infantryDescriptor)
            <= EffectiveDamageAgainst(AmmoIds.IonBeam, tankDescriptor))
        {
            throw new InvalidOperationException("ion beam should favor light targets over medium armored targets");
        }

        if (unitDesignRuntimeDefinitions.Any(definition =>
            definition.ArmorTag is not (ArmorTag.Infantry or ArmorTag.Vehicle or ArmorTag.Aircraft))
            || unitDesignRuntimeDefinitions.Any(definition => definition.MovementDomain == MovementDomain.Air && definition.ArmorTag != ArmorTag.Aircraft))
        {
            throw new InvalidOperationException("UnitDesign runtime definitions should expose infantry, vehicle, or aircraft armor tags matching movement domains");
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

        foreach (var ammo in WeaponCatalog.AmmoDefinitions.Values)
        {
            if (!Enum.GetValues<UnitWeightClass>().All(weight => ammo.DamageProfile.WeightMultipliers.ContainsKey(weight)))
            {
                throw new InvalidOperationException($"{ammo.Id} should define weight-class damage multipliers");
            }

            if (!Enum.GetValues<ArmorTag>().All(tag => ammo.DamageProfile.ArmorMultipliers.ContainsKey(tag)))
            {
                throw new InvalidOperationException($"{ammo.Id} should define armor-tag damage multipliers");
            }
        }

        if (WeaponCatalog.AmmoDefinitions[AmmoIds.SeekerRocket].DamageProfile.Multiplier(UnitWeightClass.Medium, MovementDomain.Air, ArmorTag.Aircraft)
            <= WeaponCatalog.AmmoDefinitions[AmmoIds.SeekerRocket].DamageProfile.Multiplier(UnitWeightClass.Medium, MovementDomain.Land, ArmorTag.Vehicle))
        {
            throw new InvalidOperationException("seeker rockets should have a domain/tag bonus against aircraft");
        }

        var hqSpec = BuildSpecCatalog.For(BuildingDesignIds.Headquarters);
        var lightRepeaterWeapon = WeaponCatalog.WeaponDefinitions[WeaponIds.LightRepeater];
        if (!WeaponCanTarget(lightRepeaterWeapon, aircraftDescriptor)
            || WeaponTargetPriority(lightRepeaterWeapon, infantryDescriptor) <= WeaponTargetPriority(lightRepeaterWeapon, aircraftDescriptor)
            || WeaponTargetPriority(lightRepeaterWeapon, aircraftDescriptor) <= WeaponTargetPriority(lightRepeaterWeapon, tankDescriptor)
            || WeaponTargetPriority(lightRepeaterWeapon, hqSpec) >= WeaponTargetPriority(lightRepeaterWeapon, tankDescriptor))
        {
            throw new InvalidOperationException("light repeater target profile should prefer light ground units, allow weak aircraft engagement, and de-prioritize vehicles/structures");
        }

        if (WeaponCanTarget(WeaponCatalog.WeaponDefinitions[WeaponIds.VectorCannon], aircraftDescriptor))
        {
            throw new InvalidOperationException("tank cannon target profile should not allow aircraft engagement in weapon V1");
        }

        if (EffectiveDamageAgainst(AmmoIds.NeedleDart, hqSpec)
            >= EffectiveDamageAgainst(AmmoIds.NeedleDart, infantryDescriptor))
        {
            throw new InvalidOperationException("needle dart should be weaker against structure armor than infantry armor");
        }

    }
}
