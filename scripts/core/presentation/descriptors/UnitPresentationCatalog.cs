namespace ProceduralRts.Core;

public static class UnitPresentationCatalog
{
    public static UnitSpecPresentationDescriptor ForDesign(string designId)
    {
        return ForSpec(UnitDesignCatalog.Spec(designId));
    }

    public static UnitSpecPresentationDescriptor ForSpec(UnitSpec spec)
    {
        return new UnitSpecPresentationDescriptor(
            spec.Id,
            spec.NameKey,
            spec.RoleKey,
            spec.ShortCode,
            spec.Icon,
            "unit",
            SoftOldCityPalette.FactionColor(spec.Faction),
            spec.Art,
            spec.Art.StatusGlyph == IconGlyph.None ? spec.Icon : spec.Art.StatusGlyph);
    }

    public static ProductionPresentationDescriptor ForProductionSpec(UnitSpec spec)
    {
        if (spec.Production is null)
        {
            throw new InvalidOperationException($"UnitDesign '{spec.Id}' cannot be used as a production presentation because it has no ProductionSpec.");
        }

        var unit = ForSpec(spec);
        return new ProductionPresentationDescriptor(
            TooltipKey(spec.Production.Category),
            unit.ShortCode,
            unit.Icon,
            unit.Accent,
            unit.RoleGlyph,
            spec.Production.Category,
            spec.Id);
    }

    private static string TooltipKey(ProductionCategory category)
    {
        return category switch
        {
            ProductionCategory.Infantry => "production.infantry.tooltip",
            ProductionCategory.Vehicle => "production.tank.tooltip",
            ProductionCategory.Economy => "production.harvester.tooltip",
            _ => "production.infantry.tooltip",
        };
    }
}
