namespace ProceduralRts.Core;

public abstract class WeaponDesign
{
    public virtual string Id => WeaponCatalog.IdFor(Kind);

    public virtual WeaponKind Kind =>
        throw new InvalidOperationException($"{GetType().Name} has no WeaponKind alias.");

    public abstract WeaponDefinition ToDefinition();
}
