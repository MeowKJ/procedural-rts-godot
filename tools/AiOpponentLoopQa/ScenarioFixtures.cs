using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Tools.AiOpponentLoopQa;

internal static partial class AiOpponentLoopQaProgram
{
    private static BaseRuntime BuildRuntimeBase(
        UnitBattlefield battlefield,
        PlayerSlotId slot,
        UnitFactionId faction,
        Vector2 center,
        float facing,
        int idBase)
    {
        var hq = AddBuilding(battlefield, idBase, BuildingDesignIds.Headquarters, slot, faction, center, facing);
        AddBuilding(battlefield, idBase + 1, BuildingDesignIds.PowerPlant, slot, faction, center + new Vector2(0, -230), facing);
        AddBuilding(battlefield, idBase + 2, BuildingDesignIds.Refinery, slot, faction, center + new Vector2(190 * MathF.Cos(facing), 190 * MathF.Sin(facing) - 115), facing);
        AddBuilding(battlefield, idBase + 3, BuildingDesignIds.Barracks, slot, faction, center + new Vector2(0, -390), facing);
        AddBuilding(battlefield, idBase + 4, BuildingDesignIds.VehicleFactory, slot, faction, center + new Vector2(0, 250), facing);
        var turret = AddBuilding(battlefield, idBase + 5, BuildingDesignIds.GroundTurret, slot, faction, center + new Vector2(slot == PlayerSlotId.Two ? -360 : 360, 0), facing);
        return new BaseRuntime(hq, turret);
    }

    private static UnitBattlefieldBuildingSnapshot AddBuilding(
        UnitBattlefield battlefield,
        int id,
        string kind,
        PlayerSlotId slot,
        UnitFactionId faction,
        Vector2 position,
        float facing)
    {
        return battlefield.UpsertBuildingTarget(
            id,
            kind,
            slot,
            faction,
            position,
            facing,
            BuildSpecCatalog.For(kind).MaxHp);
    }

    private static ResourceFieldModel ResourceField(int id, MapResourceNodeSpec resource)
    {
        return new ResourceFieldModel
        {
            Id = id,
            Position = resource.Position.ToVector2(),
            Radius = resource.Radius,
            MaxAmount = resource.Amount,
            Amount = resource.Amount,
            Accent = resource.Accent.ToColor(),
        };
    }

    private static void SpawnMapRoster(
        UnitBattlefield battlefield,
        MapSpec map,
        OwnerId ownerId,
        PlayerSlotId slot)
    {
        foreach (var spawn in map.Units.Where(unit => unit.OwnerId == ownerId))
        {
            battlefield.Spawn(spawn.DesignId, slot, spawn.Position.ToVector2(), spawn.Facing);
        }
    }

    private static IReadOnlyList<UnitInstance> SpawnPlayerRaiders(
        UnitBattlefield battlefield,
        UnitFactionId faction,
        Vector2 center)
    {
        var raiders = new List<UnitInstance>();
        var pattern = faction == UnitFactionId.Dog
            ? new[] { "dog.patrol_vehicle", "dog.patrol_vehicle", "dog.rocket", "dog.infantry" }
            : new[] { "cat.scout_car", "cat.scout_car", "cat.tank", "cat.basic" };
        for (var index = 0; index < pattern.Length; index++)
        {
            raiders.Add(battlefield.Spawn(
                pattern[index],
                PlayerSlotId.One,
                center + new Vector2(index * 42, (index % 2 == 0 ? -1 : 1) * 38),
                0));
        }

        return raiders;
    }

    private static int AssignIdleHarvesters(
        UnitBattlefield battlefield,
        PlayerSlotId slot,
        ResourceFieldModel field,
        HashSet<int> assignedHarvesters)
    {
        var candidates = battlefield.Units
            .Where(unit => unit.PlayerSlotId == slot)
            .Where(unit => unit.Hp > 0)
            .Where(unit => unit.Spec.RoleTags.Contains(UnitRoleTag.Economy))
            .Where(unit => unit.HarvesterMode == HarvesterMode.Idle || !assignedHarvesters.Contains(unit.Id))
            .OrderBy(unit => unit.Id)
            .ToList();
        var assignments = 0;
        foreach (var harvester in candidates)
        {
            battlefield.SelectUnitsByIds(slot, [harvester.Id]);
            if (battlefield.CommandHarvestSelected(slot, field, out _))
            {
                assignedHarvesters.Add(harvester.Id);
                assignments++;
            }
        }

        return assignments;
    }

    private static bool IsCombat(UnitInstance unit, PlayerSlotId slot)
    {
        return unit.PlayerSlotId == slot
            && unit.Hp > 0
            && !unit.Spec.RoleTags.Contains(UnitRoleTag.Economy);
    }
}
