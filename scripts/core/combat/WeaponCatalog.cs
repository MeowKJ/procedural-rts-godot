using System.Reflection;

namespace ProceduralRts.Core;

public static class WeaponCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, WeaponDefinition>> DiscoveredWeaponDefinitions = new(DiscoverWeapons);
    private static readonly Lazy<IReadOnlyDictionary<string, AmmoDefinition>> DiscoveredAmmoDefinitions = new(DiscoverAmmo);
    public static IReadOnlyDictionary<string, WeaponDefinition> WeaponDefinitions => DiscoveredWeaponDefinitions.Value;

    public static IReadOnlyDictionary<string, AmmoDefinition> AmmoDefinitions => DiscoveredAmmoDefinitions.Value;

    private static IReadOnlyDictionary<string, WeaponDefinition> DiscoverWeapons()
    {
        return DiscoverWeaponsFrom(typeof(WeaponDesign).Assembly);
    }

    public static IReadOnlyDictionary<string, WeaponDefinition> DiscoverWeaponsFrom(params Assembly[] assemblies)
    {
        var ammoDefinitions = DiscoverAmmoFrom(assemblies);
        var definitions = DiscoverDesigns<WeaponDesign>(assemblies)
            .OrderBy(design => design.Id, StringComparer.Ordinal)
            .Select(design => design.ToDefinition())
            .ToDictionary(definition => definition.Id, StringComparer.Ordinal);

        foreach (var weapon in definitions.Values)
        {
            if (!ammoDefinitions.ContainsKey(weapon.AmmoId)
                && !AmmoDefinitions.ContainsKey(weapon.AmmoId))
            {
                throw new InvalidOperationException($"{weapon.Id} references missing ammo {weapon.AmmoId}.");
            }
        }

        return definitions;
    }

    private static IReadOnlyDictionary<string, AmmoDefinition> DiscoverAmmo()
    {
        return DiscoverAmmoFrom(typeof(AmmoDesign).Assembly);
    }

    public static IReadOnlyDictionary<string, AmmoDefinition> DiscoverAmmoFrom(params Assembly[] assemblies)
    {
        return DiscoverDesigns<AmmoDesign>(assemblies)
            .OrderBy(design => design.Id, StringComparer.Ordinal)
            .Select(design => design.ToDefinition())
            .ToDictionary(definition => definition.Id, StringComparer.Ordinal);
    }

    private static IEnumerable<TDesign> DiscoverDesigns<TDesign>(IReadOnlyList<Assembly> assemblies)
    {
        return assemblies
            .SelectMany(assembly => assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(TDesign).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) is not null)
            .Select(type => (TDesign)Activator.CreateInstance(type)!));
    }
}
