namespace ProceduralRts.Core;

public sealed record CounterRule
{
    public float Multiplier { get; init; }
    public TargetTrait? Trait { get; init; }
    public UnitRoleTag? Role { get; init; }
    public ArmorTag? Armor { get; init; }
    public MovementDomain? Domain { get; init; }
    public UnitWeightClass? Weight { get; init; }

    public CounterRule(
        float Multiplier,
        TargetTrait? Trait = null,
        UnitRoleTag? Role = null,
        ArmorTag? Armor = null,
        MovementDomain? Domain = null,
        UnitWeightClass? Weight = null)
    {
        if (Multiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Multiplier), "Counter rule multipliers must be positive.");
        }

        this.Multiplier = Multiplier;
        this.Trait = Trait;
        this.Role = Role;
        this.Armor = Armor;
        this.Domain = Domain;
        this.Weight = Weight;
    }

    public bool Matches(
        TargetTraitProfile? targetTraits,
        UnitWeightClass weightClass,
        MovementDomain movementDomain,
        ArmorTag armorTag)
    {
        return (Trait is null || targetTraits?.HasTrait(Trait.Value) == true)
            && (Role is null || targetTraits?.HasRole(Role.Value) == true)
            && (Armor is null || Armor.Value == armorTag)
            && (Domain is null || Domain.Value == movementDomain)
            && (Weight is null || Weight.Value == weightClass);
    }
}

public sealed record CounterRuleProfile
{
    public static CounterRuleProfile Neutral { get; } = new();

    public IReadOnlyList<CounterRule> Rules { get; }

    public CounterRuleProfile(IReadOnlyList<CounterRule>? Rules = null)
    {
        this.Rules = Rules is null ? [] : Rules.ToArray();
    }

    public float MultiplierFor(
        TargetTraitProfile? targetTraits,
        UnitWeightClass weightClass,
        MovementDomain movementDomain,
        ArmorTag armorTag)
    {
        var multiplier = 1f;
        foreach (var rule in Rules)
        {
            if (rule.Matches(targetTraits, weightClass, movementDomain, armorTag))
            {
                multiplier *= rule.Multiplier;
            }
        }

        return multiplier;
    }
}
