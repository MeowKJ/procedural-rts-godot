namespace ProceduralRts.Core;

public abstract class WeaponDesign
{
    public abstract string Id { get; }

    public abstract WeaponDefinition ToDefinition();
}
