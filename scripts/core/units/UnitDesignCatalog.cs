namespace ProceduralRts.Core;

public static class UnitDesignCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, UnitDesign>> DiscoveredDesigns = new(DiscoverDesigns);

    public static IReadOnlyDictionary<string, UnitDesign> Designs => DiscoveredDesigns.Value;

    public static IReadOnlyDictionary<string, UnitDesign> DiscoverDesignsFrom(params System.Reflection.Assembly[] assemblies)
    {
        return DiscoverDesigns(assemblies);
    }

    public static UnitSpec Spec<TDesign>()
        where TDesign : UnitDesign, new()
    {
        return new TDesign().ToSpec();
    }

    public static UnitDesign Design<TDesign>()
        where TDesign : UnitDesign, new()
    {
        return new TDesign();
    }

    public static UnitSpec Spec(string id)
    {
        return Designs.TryGetValue(id, out var design)
            ? design.ToSpec()
            : throw new InvalidOperationException($"Unit design '{id}' is not registered.");
    }

    public static IReadOnlyList<UnitDesign> ForRoster(UnitRosterProfile roster)
    {
        return roster.Filter(Designs.Values);
    }

    private static IReadOnlyDictionary<string, UnitDesign> DiscoverDesigns()
    {
        return DiscoverDesigns(typeof(UnitDesign).Assembly);
    }

    private static IReadOnlyDictionary<string, UnitDesign> DiscoverDesigns(params System.Reflection.Assembly[] assemblies)
    {
        return assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => !type.IsAbstract && typeof(UnitDesign).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) is not null)
            .Select(type => (UnitDesign)Activator.CreateInstance(type)!)
            .OrderBy(design => design.Id, StringComparer.Ordinal)
            .ToDictionary(design => design.Id, StringComparer.Ordinal);
    }
}
