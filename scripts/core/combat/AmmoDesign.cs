namespace ProceduralRts.Core;

public abstract class AmmoDesign
{
    public virtual string Id => WeaponCatalog.IdFor(Kind);

    public virtual AmmoKind Kind =>
        throw new InvalidOperationException($"{GetType().Name} has no AmmoKind alias.");

    public abstract AmmoDefinition ToDefinition();
}
