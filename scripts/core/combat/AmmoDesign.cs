namespace ProceduralRts.Core;

public abstract class AmmoDesign
{
    public abstract string Id { get; }

    public abstract AmmoDefinition ToDefinition();
}
