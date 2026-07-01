static partial class Program
{
    private static void AssertUnitDesignRosters()
    {
        var requiredDogUnitDesignIds = RequiredDogUnitDesignIds();
        var expectedDogPlayableDesignIds = ExpectedDogPlayableDesignIds();
        var expectedCatPlayableDesignIds = ExpectedCatPlayableDesignIds();

        var dogT1Roster = UnitDesignCatalog.ForRoster(UnitRosters.DogT1);
        var dogT1VehicleRoster = UnitDesignCatalog.ForRoster(UnitRosters.DogT1Vehicles);
        var dogDesignFactionRoster = UnitDesignFactionRosterCatalog.For(UnitFactionId.Dog);
        var catDesignFactionRoster = UnitDesignFactionRosterCatalog.For(UnitFactionId.Cat);
        var dogRuntimeStart = UnitDesignRuntimeLoadouts.StartingUnits(UnitFactionId.Dog);
        var catRuntimeStart = UnitDesignRuntimeLoadouts.StartingUnits(UnitFactionId.Cat);
        if (dogT1Roster.Count < requiredDogUnitDesignIds.Length
            || dogT1Roster.Any(design => design.Faction != UnitFactionId.Dog || design.Stats.TechTier > 1)
            || dogT1VehicleRoster.Count == 0
            || dogT1VehicleRoster.Any(design => !design.RoleTags.Contains(UnitRoleTag.Vehicle)))
        {
            throw new InvalidOperationException("unit roster profiles should filter inherited unit designs by faction, tech tier, and role metadata outside unit instances");
        }

        var expectedDogStartingDesignIds = new[]
        {
            "dog.guard_tank",
            "dog.guard_tank",
            "dog.patrol_vehicle",
            "dog.infantry",
            "dog.infantry",
            "dog.rocket",
            "dog.harvester",
        };
        var expectedCatStartingDesignIds = new[]
        {
            "cat.tank",
            "cat.tank",
            "cat.scout_car",
            "cat.basic",
            "cat.basic",
            "cat.basic",
            "cat.harvester",
        };
        if (!dogDesignFactionRoster.PlayableDesignIds.SequenceEqual(expectedDogPlayableDesignIds))
        {
            throw new InvalidOperationException($"Dog UnitDesign playable roster mismatch: {string.Join(", ", dogDesignFactionRoster.PlayableDesignIds)}");
        }

        if (!catDesignFactionRoster.PlayableDesignIds.SequenceEqual(expectedCatPlayableDesignIds))
        {
            throw new InvalidOperationException($"Cat UnitDesign playable roster mismatch: {string.Join(", ", catDesignFactionRoster.PlayableDesignIds)}");
        }

        if (!dogDesignFactionRoster.StartingDesignIds.SequenceEqual(expectedDogStartingDesignIds)
            || !catDesignFactionRoster.StartingDesignIds.SequenceEqual(expectedCatStartingDesignIds)
            || !dogRuntimeStart.Select(spawn => spawn.DesignId).SequenceEqual(dogDesignFactionRoster.StartingDesignIds)
            || !catRuntimeStart.Select(spawn => spawn.DesignId).SequenceEqual(catDesignFactionRoster.StartingDesignIds))
        {
            throw new InvalidOperationException("UnitDesign runtime starting loadouts should own the Dog and Cat starting design ids without FactionCatalog StartingUnits");
        }

        if (dogDesignFactionRoster.PlayableDesignIds.Select(UnitDesignCatalog.Spec).Any(spec => spec.Faction != UnitFactionId.Dog || spec.Production is null)
            || catDesignFactionRoster.PlayableDesignIds.Select(UnitDesignCatalog.Spec).Any(spec => spec.Faction != UnitFactionId.Cat || spec.Production is null)
            || dogDesignFactionRoster.PlayableDesignIds.Select(UnitDesignCatalog.Spec).Select(spec => spec.Stats.TechTier).Distinct().Count() < 3
            || catDesignFactionRoster.PlayableDesignIds.Select(UnitDesignCatalog.Spec).Select(spec => spec.Stats.TechTier).Distinct().Count() < 3
            || dogDesignFactionRoster.StartingDesignIds.Any(designId => !dogDesignFactionRoster.PlayableDesignIds.Contains(designId))
            || catDesignFactionRoster.StartingDesignIds.Any(designId => !catDesignFactionRoster.PlayableDesignIds.Contains(designId))
            || UnitDesignFactionRosterCatalog.ProductionDesignId(UnitFactionId.Dog, UnitDesignCatalog.Spec("dog.patrol_vehicle").Production!) != "dog.guard_tank"
            || UnitDesignFactionRosterCatalog.ProductionDesignId(UnitFactionId.Cat, UnitDesignCatalog.Spec("cat.scout_car").Production!) != "cat.tank")
        {
            throw new InvalidOperationException("UnitDesign faction roster bridge should expose playable design ids from UnitSpec production authoring");
        }

        if (dogRuntimeStart.Count == 0
            || catRuntimeStart.Count == 0
            || dogRuntimeStart.Any(spawn => UnitDesignCatalog.Spec(spawn.DesignId).Faction != UnitFactionId.Dog)
            || catRuntimeStart.Any(spawn => UnitDesignCatalog.Spec(spawn.DesignId).Faction != UnitFactionId.Cat)
            || UnitDesignRuntimeLoadouts.ProductionDesignId(UnitFactionId.Dog, ProductionKind.LightTank) != "dog.guard_tank"
            || UnitDesignRuntimeLoadouts.ProductionDesignId(UnitFactionId.Cat, ProductionKind.LightTank) != "cat.tank"
            || UnitDesignRuntimeLoadouts.ProductionDesignId(UnitFactionId.Cat, ProductionKind.Harvester) != "cat.harvester")
        {
            throw new InvalidOperationException("unit design runtime loadouts should define starting and production units directly with UnitDesign ids per faction");
        }

        if (UnitDesignCatalog.Designs.Values.Any(design => !design.Art.PlayerColorZones.Any())
            || UnitDesignCatalog.Designs.Values.Any(design => design.Art.Layers.All(layer => layer.Zone != ArtLayerZone.FactionMark))
            || UnitDesignCatalog.Designs.Values.Any(design => design.Art.Layers.Any(layer => layer.ColorRole == ColorRole.Owner
                && layer.Zone is not ArtLayerZone.PlayerStripe and not ArtLayerZone.PlayerBadge)))
        {
            throw new InvalidOperationException("unit art recipes should expose explicit player sticker zones and stable faction identity marks");
        }
    }
}
