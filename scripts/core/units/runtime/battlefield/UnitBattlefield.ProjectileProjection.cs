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

    public void ProjectileProjections(List<ProjectilePresentationProjection> result)
    {
        ProjectileProjections(PlayerSlotId.One, result);
    }

    public void ProjectileProjections(PlayerSlotId viewer, List<ProjectilePresentationProjection> result)
    {
        ProjectilePresentationProjector.ProjectInto(_entityWorld, viewer, result);
    }

    public int ProjectileProjectionCount()
    {
        return ProjectilePresentationProjector.Count(_entityWorld);
    }
}
