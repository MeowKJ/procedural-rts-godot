namespace ProceduralRts.Core;

public enum TargetTrait
{
    Mechanical,
    Biological,
    Shielded,
    Siege,
    Harvester,
    Repairing,
    Stealthed,
    MoonshadowMarked
}

public sealed record TargetTraitProfile
{
    public static TargetTraitProfile Neutral { get; } = new();

    public IReadOnlySet<TargetTrait> Traits { get; }
    public IReadOnlySet<UnitRoleTag> RoleTags { get; }

    public TargetTraitProfile(
        IReadOnlySet<TargetTrait>? Traits = null,
        IReadOnlySet<UnitRoleTag>? RoleTags = null)
    {
        this.Traits = Traits is null ? new HashSet<TargetTrait>() : new HashSet<TargetTrait>(Traits);
        this.RoleTags = RoleTags is null ? new HashSet<UnitRoleTag>() : new HashSet<UnitRoleTag>(RoleTags);
    }

    public static TargetTraitProfile FromRoleTags(
        IReadOnlySet<UnitRoleTag> roleTags,
        TargetTraitProfile? baseProfile = null)
    {
        var traits = baseProfile is null ? new HashSet<TargetTrait>() : new HashSet<TargetTrait>(baseProfile.Traits);
        var roles = baseProfile is null ? new HashSet<UnitRoleTag>() : new HashSet<UnitRoleTag>(baseProfile.RoleTags);
        foreach (var role in roleTags)
        {
            roles.Add(role);
            AddInferredTrait(role, traits);
        }

        return new TargetTraitProfile(traits, roles);
    }

    public static TargetTraitProfile FromTags(
        IReadOnlySet<string> tags,
        TargetTraitProfile? baseProfile = null)
    {
        var roles = new HashSet<UnitRoleTag>();
        foreach (var tag in tags)
        {
            if (Enum.TryParse<UnitRoleTag>(tag, out var role))
            {
                roles.Add(role);
            }
        }

        return roles.Count == 0 && baseProfile is null
            ? Neutral
            : FromRoleTags(roles, baseProfile);
    }

    public bool HasTrait(TargetTrait trait)
    {
        return Traits.Contains(trait);
    }

    public bool HasRole(UnitRoleTag role)
    {
        return RoleTags.Contains(role);
    }

    private static void AddInferredTrait(UnitRoleTag role, HashSet<TargetTrait> traits)
    {
        switch (role)
        {
            case UnitRoleTag.Infantry:
                traits.Add(TargetTrait.Biological);
                break;
            case UnitRoleTag.Vehicle:
            case UnitRoleTag.Aircraft:
                traits.Add(TargetTrait.Mechanical);
                break;
            case UnitRoleTag.Shield:
                traits.Add(TargetTrait.Shielded);
                break;
            case UnitRoleTag.Siege:
                traits.Add(TargetTrait.Siege);
                break;
            case UnitRoleTag.Worker:
            case UnitRoleTag.Economy:
                traits.Add(TargetTrait.Harvester);
                break;
        }
    }
}
