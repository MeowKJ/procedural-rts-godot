namespace ProceduralRts.Core;

public abstract class BuildingDesign
{
    public abstract string Kind { get; }

    public abstract int SortOrder { get; }

    public abstract BuildSpec ToSpec();
}
