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
            .Select(design => design.ToSpec())
            .ToDictionary(spec => spec.Kind, StringComparer.Ordinal);
    }
}
