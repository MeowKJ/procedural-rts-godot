namespace ProceduralRts.Core;

public static class BuildSpecCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, BuildSpec>> DiscoveredDefinitions = new(DiscoverDefinitions);

    public static IReadOnlyDictionary<string, BuildSpec> Definitions => DiscoveredDefinitions.Value;

    public static IReadOnlyDictionary<string, BuildSpec> DiscoverDefinitionsFrom(params System.Reflection.Assembly[] assemblies)
    {
        return DiscoverDefinitions(assemblies);
    }

    public static BuildSpec For(string kind)
    {
        return Definitions[kind];
    }

    public static BuildConstructionPolicy ConstructionPolicyFor(string kind)
    {
        return For(kind).ConstructionMethods;
    }

    public static ConstructionMethod ConstructionMethodFor(string kind, UnitFactionId faction)
    {
        return For(kind).ConstructionMethodFor(faction);
    }

    public static ConstructionMethod ConstructionMethod(string kind, ConstructionMethodKind method)
    {
        return For(kind).ConstructionMethod(method);
    }

    private static IReadOnlyDictionary<string, BuildSpec> DiscoverDefinitions()
    {
        return DiscoverDefinitions(typeof(BuildingDesign).Assembly);
    }

    private static IReadOnlyDictionary<string, BuildSpec> DiscoverDefinitions(params System.Reflection.Assembly[] assemblies)
    {
        return assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => !type.IsAbstract && typeof(BuildingDesign).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) is not null)
            .Select(type => (BuildingDesign)Activator.CreateInstance(type)!)
            .OrderBy(design => design.SortOrder)
            .ThenBy(design => design.Kind, StringComparer.Ordinal)
            .Select(design => Validate(design.ToSpec()))
            .ToDictionary(spec => spec.Kind, StringComparer.Ordinal);
    }

    private static BuildSpec Validate(BuildSpec spec)
    {
        if (!spec.FootprintCells.IsValid)
        {
            throw new InvalidOperationException($"BuildSpec '{spec.Kind}' must declare a positive placement footprint.");
        }

        var productionEgressCount = 0;
        var refineryDockCount = 0;
        for (var index = 0; index < spec.PlacementReservations.Count; index++)
        {
            var reservation = spec.PlacementReservations[index];
            ValidateReservationRange(spec, reservation);
            if (reservation.Kind == PlacementReservationKind.ProductionEgress)
            {
                productionEgressCount++;
            }
            else if (reservation.Kind == PlacementReservationKind.RefineryDock)
            {
                refineryDockCount++;
            }

            if (RangesOverlap(
                    reservation.Column,
                    (long)reservation.Column + reservation.WidthCells,
                    0,
                    spec.FootprintCells.WidthCells)
                && RangesOverlap(
                    reservation.Row,
                    (long)reservation.Row + reservation.HeightCells,
                    0,
                    spec.FootprintCells.HeightCells))
            {
                throw new InvalidOperationException($"BuildSpec '{spec.Kind}' reservation {index} overlaps its hard footprint.");
            }

            for (var otherIndex = 0; otherIndex < index; otherIndex++)
            {
                var other = spec.PlacementReservations[otherIndex];
                if (RangesOverlap(
                        reservation.Column,
                        (long)reservation.Column + reservation.WidthCells,
                        other.Column,
                        (long)other.Column + other.WidthCells)
                    && RangesOverlap(
                        reservation.Row,
                        (long)reservation.Row + reservation.HeightCells,
                        other.Row,
                        (long)other.Row + other.HeightCells))
                {
                    throw new InvalidOperationException($"BuildSpec '{spec.Kind}' reservations {otherIndex} and {index} overlap.");
                }
            }
        }

        var expectedProductionEgressCount = spec.Kind is BuildingDesignIds.Barracks
            or BuildingDesignIds.VehicleFactory
            or BuildingDesignIds.Airfield
                ? 1
                : 0;
        var expectedRefineryDockCount = spec.Kind == BuildingDesignIds.Refinery ? 1 : 0;
        if (productionEgressCount != expectedProductionEgressCount
            || refineryDockCount != expectedRefineryDockCount
            || spec.PlacementReservations.Count != productionEgressCount + refineryDockCount)
        {
            throw new InvalidOperationException(
                $"BuildSpec '{spec.Kind}' must declare {expectedProductionEgressCount} production egress and {expectedRefineryDockCount} refinery dock reservations.");
        }

        return spec;
    }

    private static void ValidateReservationRange(BuildSpec spec, PlacementReservationSpec reservation)
    {
        if (reservation.WidthCells <= 0 || reservation.HeightCells <= 0)
        {
            throw new InvalidOperationException($"BuildSpec '{spec.Kind}' reservations must have positive dimensions.");
        }

        var endColumn = (long)reservation.Column + reservation.WidthCells;
        var endRow = (long)reservation.Row + reservation.HeightCells;
        if (endColumn is < int.MinValue or > int.MaxValue
            || endRow is < int.MinValue or > int.MaxValue)
        {
            throw new InvalidOperationException($"BuildSpec '{spec.Kind}' reservation range overflows the placement lattice.");
        }
    }

    private static bool RangesOverlap(long startA, long endA, long startB, long endB)
    {
        return startA < endB && endA > startB;
    }
}
