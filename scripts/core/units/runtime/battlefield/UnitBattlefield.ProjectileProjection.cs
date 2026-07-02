namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public IReadOnlyList<ProjectilePresentationProjection> ProjectileProjections()
    {
        return ProjectileProjections(PlayerSlotId.One);
    }

    public IReadOnlyList<ProjectilePresentationProjection> ProjectileProjections(PlayerSlotId viewer)
    {
        return ProjectilePresentationProjector.Project(_entityWorld, viewer);
    }

    public int ProjectileProjectionCount()
    {
        return ProjectilePresentationProjector.Count(_entityWorld);
    }
}
