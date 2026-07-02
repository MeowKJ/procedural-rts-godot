using ProceduralRts.Core;

namespace ProceduralRts.Controllers;

public partial class ControlGroupController
{
    private readonly List<ControlGroupSnapshot> _snapshotBuffer = [];
    private readonly HashSet<int> _snapshotSelectedIds = [];

    public IReadOnlyList<ControlGroupSnapshot> Snapshots()
    {
        _snapshotBuffer.Clear();
        CollectSnapshotSelectedIds();

        if (UseUnitBattlefieldGroups())
        {
            CollectUnitBattlefieldSnapshots();
        }
        else
        {
            CollectLegacySnapshots();
        }

        return _snapshotBuffer;
    }

    private void CollectSnapshotSelectedIds()
    {
        _snapshotSelectedIds.Clear();
        if (UseUnitBattlefieldGroups())
        {
            foreach (var unit in UnitBattlefield!.Units)
            {
                if (unit.PlayerSlotId == LocalPlayerSlotId && unit.Selected)
                {
                    _snapshotSelectedIds.Add(unit.Id);
                }
            }

            return;
        }

        foreach (var unit in State.Units)
        {
            if (unit.Owner == ProceduralRts.Core.Owner.Player && unit.Selected)
            {
                _snapshotSelectedIds.Add(unit.Id);
            }
        }
    }

    private void CollectLegacySnapshots()
    {
        for (var groupNumber = 1; groupNumber <= 9; groupNumber++)
        {
            _groups.TryGetValue(groupNumber, out var storedIds);
            var infantryCount = 0;
            var vehicleCount = 0;
            var economyCount = 0;
            var liveCount = 0;
            var allLiveSelected = true;

            if (storedIds is not null)
            {
                foreach (var unitId in storedIds)
                {
                    var unit = State.UnitById(unitId);
                    if (unit is null || unit.Owner != ProceduralRts.Core.Owner.Player || unit.Hp <= 0)
                    {
                        continue;
                    }

                    liveCount++;
                    allLiveSelected &= _snapshotSelectedIds.Contains(unit.Id);
                    CountSnapshotSpec(unit.Spec, ref infantryCount, ref vehicleCount, ref economyCount);
                }
            }

            AddSnapshot(groupNumber, infantryCount, vehicleCount, economyCount, liveCount, allLiveSelected);
        }
    }

    private void CollectUnitBattlefieldSnapshots()
    {
        for (var groupNumber = 1; groupNumber <= 9; groupNumber++)
        {
            _groups.TryGetValue(groupNumber, out var storedIds);
            var infantryCount = 0;
            var vehicleCount = 0;
            var economyCount = 0;
            var liveCount = 0;
            var allLiveSelected = true;

            if (storedIds is not null)
            {
                foreach (var unitId in storedIds)
                {
                    var unit = UnitBattlefieldUnitById(unitId);
                    if (unit is null || unit.PlayerSlotId != LocalPlayerSlotId || unit.Hp <= 0)
                    {
                        continue;
                    }

                    liveCount++;
                    allLiveSelected &= _snapshotSelectedIds.Contains(unit.Id);
                    CountSnapshotSpec(unit.Spec, ref infantryCount, ref vehicleCount, ref economyCount);
                }
            }

            AddSnapshot(groupNumber, infantryCount, vehicleCount, economyCount, liveCount, allLiveSelected);
        }
    }

    private void AddSnapshot(
        int groupNumber,
        int infantryCount,
        int vehicleCount,
        int economyCount,
        int liveCount,
        bool allLiveSelected)
    {
        var active = liveCount > 0
            && liveCount == _snapshotSelectedIds.Count
            && allLiveSelected;
        _snapshotBuffer.Add(new ControlGroupSnapshot(
            groupNumber,
            infantryCount,
            vehicleCount,
            economyCount,
            active,
            _feedbackPulses[groupNumber]));
    }

    private UnitInstance? UnitBattlefieldUnitById(int unitId)
    {
        foreach (var unit in UnitBattlefield!.Units)
        {
            if (unit.Id == unitId)
            {
                return unit;
            }
        }

        return null;
    }

    private static void CountSnapshotSpec(
        UnitSpec spec,
        ref int infantryCount,
        ref int vehicleCount,
        ref int economyCount)
    {
        var economy = IsHarvestEconomySpec(spec);
        if (economy)
        {
            economyCount++;
        }
        else if (spec.RoleTags.Contains(UnitRoleTag.Infantry))
        {
            infantryCount++;
        }
        else if (spec.RoleTags.Contains(UnitRoleTag.Vehicle))
        {
            vehicleCount++;
        }
    }

    private static bool IsHarvestEconomySpec(UnitSpec spec)
    {
        var canHarvest = false;
        foreach (var ability in spec.Abilities)
        {
            if (ability.Kind == AbilityKind.Harvest)
            {
                canHarvest = true;
                break;
            }
        }

        return canHarvest
            && (spec.RoleTags.Contains(UnitRoleTag.Economy)
                || spec.RoleTags.Contains(UnitRoleTag.Worker));
    }
}
