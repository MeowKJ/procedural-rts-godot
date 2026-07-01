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

    private static ResourceFieldModel ResourceField(int id, Vector2 position, int amount)
    {
        return new ResourceFieldModel
        {
            Id = id,
            Position = position,
            Radius = 86,
            MaxAmount = amount,
            Amount = amount,
            Accent = new Color("#f6c55c"),
        };
    }

    private static void SpawnStartingRoster(
        UnitBattlefield battlefield,
        PlayerSlotId slot,
        UnitFactionId faction,
        Vector2 center,
        int direction)
    {
        foreach (var spawn in UnitDesignRuntimeLoadouts.StartingUnits(faction))
        {
            var rotated = new Vector2(spawn.Offset.X * direction, spawn.Offset.Y);
            var facing = direction > 0 ? spawn.FacingOffset : MathF.PI - spawn.FacingOffset;
            battlefield.Spawn(spawn.DesignId, slot, center + rotated, facing);
        }
    }

    private static IReadOnlyList<UnitInstance> SpawnPlayerRaiders(UnitBattlefield battlefield, Vector2 center)
    {
        var raiders = new List<UnitInstance>();
        var pattern = new[] { "dog.patrol_vehicle", "dog.patrol_vehicle", "dog.rocket", "dog.infantry" };
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
