namespace ProceduralRts.Core;

public sealed record UnitRosterProfile(
    string Id,
    UnitFactionId? Faction = null,
    int? MaximumTechTier = null,
    IReadOnlySet<UnitRoleTag>? RequiredTags = null)
{
    public bool Allows(UnitDesign design)
    {
        if (Faction is { } faction && design.Faction != faction)
        {
            return false;
        }

        if (MaximumTechTier is { } maximumTechTier && design.Stats.TechTier > maximumTechTier)
        {
            return false;
        }

        if (RequiredTags is { Count: > 0 } requiredTags && !requiredTags.All(design.RoleTags.Contains))
        {
            return false;
        }

        return true;
    }

    public IReadOnlyList<UnitDesign> Filter(IEnumerable<UnitDesign> designs)
    {
        return designs.Where(Allows).OrderBy(design => design.Id).ToList();
    }
}
