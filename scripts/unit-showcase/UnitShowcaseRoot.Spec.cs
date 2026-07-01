namespace ProceduralRts;

public partial class UnitShowcaseRoot
{
    private static UnitSpec[] DogFaction()
    {
        return
        [
            new("D1", "Sentry", "T1 repair infantry", UnitShape.DogInfantry, Role.Repair),
            new("D2", "Charge", "T1 assault tank", UnitShape.DogTank, Role.Assault),
            new("D3", "Relay", "T1 harvester", UnitShape.UtilityTruck, Role.Harvest),
            new("D4", "Medic", "T2 repair dog", UnitShape.DogInfantry, Role.Repair),
            new("D5", "Bulwark", "T2 shield tank", UnitShape.HeavyTank, Role.Defense),
            new("D6", "Breaker", "T2 assault tank", UnitShape.DogTank, Role.Assault),
            new("D7", "Howl", "T3 bombard tank", UnitShape.Artillery, Role.Bombard),
            new("D8", "Gate", "T3 area shield", UnitShape.ShieldTank, Role.Defense),
            new("D9", "Lampwing", "T3 scout aircraft", UnitShape.Aircraft, Role.Scout),
        ];
    }

    private static UnitSpec[] CatFaction()
    {
        return
        [
            new("C1", "Alley", "T1 cheap infantry", UnitShape.CatInfantry, Role.Assault),
            new("C2", "Rocket", "T1 anti-armor", UnitShape.CatRocket, Role.Bombard),
            new("C3", "Wire", "T1 engineer", UnitShape.CatInfantry, Role.Repair),
            new("C4", "Needle", "T1 fast tank", UnitShape.CatTank, Role.Assault),
            new("C5", "Litter", "T1 harvester", UnitShape.UtilityTruck, Role.Harvest),
            new("C6", "Roof", "T2 sniper", UnitShape.CatSniper, Role.Assault),
            new("C7", "Quiet", "T2 repair tank", UnitShape.RepairTank, Role.Repair),
            new("C8", "Curtain", "T2 shield tank", UnitShape.ShieldTank, Role.Defense),
            new("C9", "Moon", "T3 bombard tank", UnitShape.Artillery, Role.Bombard),
            new("C10", "Ghost", "T3 special ops", UnitShape.CatSpecial, Role.Scout),
            new("C11", "Whisker", "T1 scout plane", UnitShape.Aircraft, Role.Scout),
            new("C12", "Claw", "T1 air fighter", UnitShape.Fighter, Role.Assault),
        ];
    }

    private enum UnitShape
    {
        DogInfantry,
        CatInfantry,
        CatRocket,
        CatSniper,
        CatSpecial,
        DogTank,
        CatTank,
        HeavyTank,
        Artillery,
        ShieldTank,
        RepairTank,
        UtilityTruck,
        Aircraft,
        Fighter,
    }

    private enum Role
    {
        Assault,
        Repair,
        Defense,
        Bombard,
        Harvest,
        Scout,
    }

    private sealed record UnitSpec(string Code, string Name, string Description, UnitShape Shape, Role Role);
}
