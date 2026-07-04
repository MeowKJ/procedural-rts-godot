static partial class Program
{
    private static void AssertFactionShapeLanguage()
    {
        var pairedDesigns = new[]
        {
            (Dog: UnitDesignIds.DogInfantry, Cat: UnitDesignIds.CatBasic, Label: "infantry"),
            (Dog: UnitDesignIds.DogGuardTank, Cat: UnitDesignIds.CatTank, Label: "vehicle"),
            (Dog: UnitDesignIds.DogHarvester, Cat: UnitDesignIds.CatHarvester, Label: "harvester"),
            (Dog: UnitDesignIds.DogSkyPatrolAircraft, Cat: UnitDesignIds.CatScoutAircraft, Label: "aircraft"),
        };

        foreach (var pair in pairedDesigns)
        {
            var dog = UnitDesignCatalog.Spec(pair.Dog);
            var cat = UnitDesignCatalog.Spec(pair.Cat);
            RequireFactionRecipe(dog, UnitFactionId.Dog, pair.Label);
            RequireFactionRecipe(cat, UnitFactionId.Cat, pair.Label);
            if (SameShapeSignature(dog.Art, cat.Art))
            {
                throw new InvalidOperationException($"{pair.Label} Dog/Cat art recipes must differ by shape and glyph, not only by palette.");
            }
        }

        var corruptionRoster = UnitDesignFactionRosterCatalog.For(UnitFactionId.Corruption);
        if (corruptionRoster.PlayableDesignIds.Count != 0
            || corruptionRoster.StartingUnits.Count != 0
            || UnitDesignCatalog.Designs.Values.Any(design => design.Faction == UnitFactionId.Corruption))
        {
            throw new InvalidOperationException("Corruption must stay a locked placeholder faction with no playable unit art roster.");
        }
    }

    private static void RequireFactionRecipe(UnitSpec spec, UnitFactionId faction, string label)
    {
        if (spec.Faction != faction)
        {
            throw new InvalidOperationException($"{spec.Id} must keep its expected {faction} faction identity.");
        }

        var recipe = spec.Art;
        var bodyPolygons = 0;
        var factionMarks = 0;
        var playerColorZones = 0;
        foreach (var layer in recipe.Layers)
        {
            if (layer.Shape.Kind == UnitShapeKind.Polygon && layer.Shape.Filled)
            {
                bodyPolygons++;
            }

            if (layer.Zone == ArtLayerZone.FactionMark)
            {
                factionMarks++;
            }

            if (layer.ColorRole == ColorRole.Owner
                || layer.Zone is ArtLayerZone.PlayerStripe or ArtLayerZone.PlayerBadge)
            {
                playerColorZones++;
            }
        }

        if (bodyPolygons == 0 || factionMarks == 0 || playerColorZones < 2 || recipe.StatusGlyph == IconGlyph.None)
        {
            throw new InvalidOperationException($"{label} {faction} art must expose body silhouette, faction shape marks, player-color zones, and a role glyph.");
        }
    }

    private static bool SameShapeSignature(UnitArtRecipe left, UnitArtRecipe right)
    {
        if (left.Layers.Count != right.Layers.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Layers.Count; index++)
        {
            if (!SameArtLayerShape(left.Layers[index], right.Layers[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool SameArtLayerShape(ArtLayer left, ArtLayer right)
    {
        return left.Binding == right.Binding
            && left.Zone == right.Zone
            && left.EnvironmentResponse == right.EnvironmentResponse
            && SameShapeLayer(left.Shape, right.Shape);
    }

    private static bool SameShapeLayer(UnitShapeLayer left, UnitShapeLayer right)
    {
        if (left.Kind != right.Kind
            || left.Role != right.Role
            || left.Filled != right.Filled
            || !Near(left.Radius, right.Radius)
            || !Near(left.Width, right.Width)
            || !Near(left.From, right.From)
            || !Near(left.To, right.To)
            || left.Points.Length != right.Points.Length)
        {
            return false;
        }

        for (var index = 0; index < left.Points.Length; index++)
        {
            if (!Near(left.Points[index], right.Points[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool Near(Vector2 left, Vector2 right)
    {
        return Near(left.X, right.X) && Near(left.Y, right.Y);
    }

    private static bool Near(float left, float right)
    {
        return MathF.Abs(left - right) < 0.001f;
    }
}
