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

    public static ProductionPresentationDescriptor For(UnitFactionId faction, ProductionKind kind)
    {
        var designId = UnitDesignRuntimeLoadouts.ProductionDesignId(faction, kind)
            ?? throw new KeyNotFoundException($"No production UnitDesign is available for {faction} / {kind}.");
        return ForProductionSpec(kind, UnitDesignCatalog.Spec(designId));
    }

    public static ProductionPresentationDescriptor ForProductionSpec(ProductionKind kind, UnitSpec spec)
    {
        if (spec.Production is null)
        {
            throw new InvalidOperationException($"UnitDesign '{spec.Id}' cannot be used as a production presentation because it has no ProductionSpec.");
        }

        var kindPresentation = Production[kind];
        var unit = ForSpec(spec);
        return new ProductionPresentationDescriptor(
            kind,
            kindPresentation.TooltipKey,
            unit.ShortCode,
            unit.Icon,
            unit.Accent,
            unit.RoleGlyph,
            spec.Production.Category,
            spec.Id);
    }

    public static readonly IReadOnlyDictionary<ProductionKind, ProductionPresentationDescriptor> Production =
        new Dictionary<ProductionKind, ProductionPresentationDescriptor>
        {
            [ProductionKind.InfantrySquad] = ProductionDescriptor(ProductionKind.InfantrySquad, "generic.infantry", "production.infantry.tooltip", ProductionCategory.Infantry),
            [ProductionKind.LightTank] = ProductionDescriptor(ProductionKind.LightTank, "generic.light_tank", "production.tank.tooltip", ProductionCategory.Vehicle),
            [ProductionKind.Harvester] = ProductionDescriptor(ProductionKind.Harvester, "generic.harvester", "production.harvester.tooltip", ProductionCategory.Economy),
        };

    public static ProductionPresentationDescriptor For(ProductionKind kind)
    {
        return Production[kind];
    }

    private static ProductionPresentationDescriptor ProductionDescriptor(ProductionKind kind, string outputDesignId, string tooltipKey, ProductionCategory category)
    {
        var unit = ForDesign(outputDesignId);
        return new ProductionPresentationDescriptor(kind, tooltipKey, unit.ShortCode, unit.Icon, unit.Accent, unit.RoleGlyph, category, outputDesignId);
    }
}
