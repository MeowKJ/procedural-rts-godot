using System.Reflection;

namespace ProceduralRts.Core;

public sealed partial class EntityWorld
{
    private readonly SortedDictionary<string, WeaponDefinition> _weaponDefinitions = new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, AmmoDefinition> _ammoDefinitions = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, WeaponDefinition> WeaponDefinitions => _weaponDefinitions;

    public IReadOnlyDictionary<string, AmmoDefinition> AmmoDefinitions => _ammoDefinitions;

    public void RegisterCombatDefinitionsFrom(Assembly assembly)
    {
        RegisterCombatDefinitions(
            WeaponCatalog.DiscoverWeaponsFrom(assembly).Values,
            WeaponCatalog.DiscoverAmmoFrom(assembly).Values);
    }

    public void RegisterCombatDefinitions(
        IEnumerable<WeaponDefinition> weapons,
        IEnumerable<AmmoDefinition> ammo)
    {
        foreach (var definition in ammo)
        {
            _ammoDefinitions[definition.Id] = definition;
        }

        foreach (var definition in weapons)
        {
            if (!_ammoDefinitions.ContainsKey(definition.AmmoId))
            {
                throw new InvalidOperationException($"{definition.Id} references missing ammo {definition.AmmoId}.");
            }

            _weaponDefinitions[definition.Id] = definition;
        }
    }

    public bool TryGetWeaponDefinition(string id, out WeaponDefinition definition)
    {
        return _weaponDefinitions.TryGetValue(id, out definition!);
    }

    public bool TryGetAmmoDefinition(string id, out AmmoDefinition definition)
    {
        return _ammoDefinitions.TryGetValue(id, out definition!);
    }
}
