namespace ProceduralRts.Core;

public static class UnitRosters
{
    public static readonly UnitRosterProfile DogT1 = new(
        "dog.t1",
        UnitFactionId.Dog,
        MaximumTechTier: 1);

    public static readonly UnitRosterProfile DogT1Vehicles = new(
        "dog.t1.vehicles",
        UnitFactionId.Dog,
        MaximumTechTier: 1,
        RequiredTags: new HashSet<UnitRoleTag> { UnitRoleTag.Vehicle });
}
