static partial class Program
{
    private static void AssertPerClassSilhouetteRules()
    {
        var infantry = UnitDesignCatalog.Spec("dog.infantry");
        var vehicle = UnitDesignCatalog.Spec("dog.guard_tank");
        var aircraft = UnitDesignCatalog.Spec("dog.sky_patrol_aircraft");

        var infantryDescriptor = UnitDesignDefinitionCatalog.ForDesign(infantry.Id);
        var vehicleDescriptor = UnitDesignDefinitionCatalog.ForDesign(vehicle.Id);
        var aircraftDescriptor = UnitDesignDefinitionCatalog.ForDesign(aircraft.Id);

        var infantryFootprint = FootprintVisualMath.StyleFor(infantryDescriptor);
        var vehicleFootprint = FootprintVisualMath.StyleFor(vehicleDescriptor);
        var aircraftFootprint = FootprintVisualMath.StyleFor(aircraftDescriptor);
        var navalPaperFootprint = FootprintVisualMath.StyleFor(vehicleDescriptor with { MovementDomain = MovementDomain.Naval });

        var infantryBounds = BodyBounds(infantry.Art);
        var vehicleBounds = BodyBounds(vehicle.Art);
        var aircraftBounds = BodyBounds(aircraft.Art);

        if (infantryDescriptor.WeightClass != UnitWeightClass.Light
            || infantryDescriptor.ArmorTag != ArmorTag.Infantry
            || infantryFootprint.MarkKind != FootprintMarkKind.Step
            || !infantry.Art.AnimationHints.Contains("light-step")
            || infantry.Art.StatusGlyph != IconGlyph.Infantry
            || infantryBounds.Size.X > 44
            || infantryBounds.Size.Y > 34)
        {
            throw new InvalidOperationException("light infantry silhouettes must stay compact, round-bodied, lightly marked, and use thin step footprints.");
        }

        if (vehicleDescriptor.ArmorTag != ArmorTag.Vehicle
            || vehicleFootprint.MarkKind != FootprintMarkKind.TwinTread
            || !vehicle.Art.AnimationHints.Contains("tracked-idle")
            || !vehicle.Art.AnimationHints.Contains("turret-follow-main")
            || vehicleBounds.Size.X <= infantryBounds.Size.X * 1.45f
            || vehicleBounds.Size.Y <= infantryBounds.Size.Y
            || CountBodyLines(vehicle.Art, ColorRole.Shadow, minWidth: 3.0f) < 2
            || CountMountLayers(vehicle.Art) < 3
            || MaxBodyOutlineWidth(vehicle.Art) < 2.4f)
        {
            throw new InvalidOperationException("tank/vehicle silhouettes must stay wide, heavy-outlined, treaded, and show a distinct turret mount.");
        }

        if (aircraftDescriptor.MovementDomain != MovementDomain.Air
            || aircraftDescriptor.ArmorTag != ArmorTag.Aircraft
            || aircraftFootprint.MarkKind != FootprintMarkKind.Contrail
            || !aircraft.Art.AnimationHints.Any(hint => hint.StartsWith("air-", StringComparison.Ordinal))
            || !aircraft.Art.AnimationHints.Contains("contrail-soft")
            || CountBodyLayers(aircraft.Art, ColorRole.Shadow) == 0
            || CountBodyLines(aircraft.Art, ColorRole.Effect, minWidth: 1.0f) < 2
            || aircraftBounds.Size.X <= vehicleBounds.Size.X)
        {
            throw new InvalidOperationException("aircraft silhouettes must float above ground tracks with soft shadow, contrail, and aircraft-specific body marks.");
        }

        var runtimeDefinitions = UnitDesignDefinitionCatalog.RuntimeDescriptors.Values;
        if (runtimeDefinitions.Any(definition => definition.MovementDomain is MovementDomain.Naval or MovementDomain.Amphibious || definition.ArmorTag == ArmorTag.Ship)
            || navalPaperFootprint.MarkKind != FootprintMarkKind.Wake
            || navalPaperFootprint.Length <= aircraftFootprint.Length)
        {
            throw new InvalidOperationException("ship silhouettes must remain paper-only while naval footprint policy reserves wake ripples.");
        }

        var headquarters = BuildSpecCatalog.For(BuildingDesignIds.Headquarters);
        var turret = BuildSpecCatalog.For(BuildingDesignIds.GroundTurret);
        var headquartersArea = headquarters.Footprint.X * headquarters.Footprint.Y;
        var turretArea = turret.Footprint.X * turret.Footprint.Y;
        if (headquarters.Category != BuildCategory.Command
            || headquarters.Icon != IconGlyph.Building
            || headquarters.RoleGlyph != IconGlyph.StanceHold
            || headquarters.Footprint.X <= 96
            || headquarters.Footprint.Y <= 80
            || turret.Category != BuildCategory.Defense
            || turret.Icon != IconGlyph.Turret
            || turret.RoleGlyph != IconGlyph.AttackMove
            || turret.WeaponId is null
            || turret.RequiredProducer != BuildingDesignIds.Headquarters
            || !turret.RequiredBuildings.Contains(BuildingDesignIds.PowerPlant)
            || turret.Footprint.X >= headquarters.Footprint.X
            || turret.Footprint.Y >= headquarters.Footprint.Y
            || turretArea >= headquartersArea * 0.45f)
        {
            throw new InvalidOperationException("building and turret specs must keep repaired-facility footprints separate from compact fixed-weapon platforms.");
        }
    }

    private static Rect2 BodyBounds(UnitArtRecipe recipe)
    {
        foreach (var layer in recipe.Layers)
        {
            if (layer.Binding.Kind == ArtBindingKind.Body
                && layer.ColorRole == ColorRole.Body
                && layer.Shape.Kind == UnitShapeKind.Polygon
                && layer.Shape.Filled)
            {
                return Bounds(layer.Shape.Points);
            }
        }

        throw new InvalidOperationException($"{recipe.Id} must include a filled body polygon.");
    }

    private static Rect2 Bounds(IReadOnlyList<Vector2> points)
    {
        if (points.Count == 0)
        {
            throw new InvalidOperationException("silhouette polygon must include points.");
        }

        var minX = points[0].X;
        var maxX = points[0].X;
        var minY = points[0].Y;
        var maxY = points[0].Y;
        for (var index = 1; index < points.Count; index++)
        {
            var point = points[index];
            minX = MathF.Min(minX, point.X);
            maxX = MathF.Max(maxX, point.X);
            minY = MathF.Min(minY, point.Y);
            maxY = MathF.Max(maxY, point.Y);
        }

        return new Rect2(minX, minY, maxX - minX, maxY - minY);
    }

    private static int CountBodyLayers(UnitArtRecipe recipe, ColorRole colorRole)
    {
        var count = 0;
        foreach (var layer in recipe.Layers)
        {
            if (layer.Binding.Kind == ArtBindingKind.Body && layer.ColorRole == colorRole)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountBodyLines(UnitArtRecipe recipe, ColorRole colorRole, float minWidth)
    {
        var count = 0;
        foreach (var layer in recipe.Layers)
        {
            if (layer.Binding.Kind == ArtBindingKind.Body
                && layer.Shape.Kind == UnitShapeKind.Line
                && layer.ColorRole == colorRole
                && layer.Shape.Width >= minWidth)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountMountLayers(UnitArtRecipe recipe)
    {
        var count = 0;
        foreach (var layer in recipe.Layers)
        {
            if (layer.Binding.Kind == ArtBindingKind.Mount)
            {
                count++;
            }
        }

        return count;
    }

    private static float MaxBodyOutlineWidth(UnitArtRecipe recipe)
    {
        var max = 0f;
        foreach (var layer in recipe.Layers)
        {
            if (layer.Binding.Kind == ArtBindingKind.Body
                && layer.Shape.Role == UnitShapeRole.AccentStroke
                && !layer.Shape.Filled)
            {
                max = MathF.Max(max, layer.Shape.Width);
            }
        }

        return max;
    }
}
