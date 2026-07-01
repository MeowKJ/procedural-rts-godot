using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Controllers;

public partial class ControlGroupController : Node
{
    private const double RecallCenterDoubleTapSeconds = 0.45;

    public required GameState State { get; init; }
    public UnitBattlefield? UnitBattlefield { get; init; }
    public PlayerSlotId LocalPlayerSlotId { get; init; } = PlayerSlotId.One;
    public Action<int>? SelectionChanged { get; init; }
    public Action<Vector2>? FocusRequested { get; init; }
    public Action<string>? StatusChanged { get; init; }

    private readonly Dictionary<int, List<int>> _groups = [];
    private readonly float[] _feedbackPulses = new float[10];
    private int? _lastRecalledGroup;
    private double _lastRecallSeconds;

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        for (var index = 1; index < _feedbackPulses.Length; index++)
        {
            _feedbackPulses[index] = Mathf.Max(0, _feedbackPulses[index] - dt * 2.8f);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo)
        {
            return;
        }

        var groupNumber = NumberFromKey(key.Keycode);
        if (groupNumber is null)
        {
            return;
        }

        if (key.CtrlPressed)
        {
            SaveGroup(groupNumber.Value);
            GetViewport().SetInputAsHandled();
            return;
        }

        RecallGroup(groupNumber.Value);
        GetViewport().SetInputAsHandled();
    }

    private void SaveGroup(int groupNumber)
    {
        var selectedIds = SelectedUnitIds().ToList();
        _groups[groupNumber] = selectedIds;
        _feedbackPulses[groupNumber] = 1;
        StatusChanged?.Invoke(GameText.Format("group.saved", groupNumber, selectedIds.Count));
    }

    private void RecallGroup(int groupNumber)
    {
        if (!_groups.TryGetValue(groupNumber, out var groupIds) || groupIds.Count == 0)
        {
            _feedbackPulses[groupNumber] = 1;
            StatusChanged?.Invoke(GameText.Format("group.empty", groupNumber));
            SelectionChanged?.Invoke(SelectUnitsByIds([]));
            RememberRecall(groupNumber);
            return;
        }

        var doubleTap = IsDoubleTapRecall(groupNumber);
        var selectedCount = SelectUnitsByIds(groupIds);
        _feedbackPulses[groupNumber] = 1;
        SelectionChanged?.Invoke(selectedCount);
        if (doubleTap && selectedCount > 0 && GroupCenter(groupIds) is { } center)
        {
            FocusRequested?.Invoke(center);
        }

        RememberRecall(groupNumber);
        StatusChanged?.Invoke(GameText.Format("group.recalled", groupNumber, selectedCount));
    }

    public IReadOnlyList<ControlGroupSnapshot> Snapshots()
    {
        if (UseUnitBattlefieldGroups())
        {
            return UnitBattlefieldSnapshots();
        }

        var selectedIds = State.SelectedUnitIds().ToHashSet();
        var snapshots = new List<ControlGroupSnapshot>(9);

        for (var groupNumber = 1; groupNumber <= 9; groupNumber++)
        {
            _groups.TryGetValue(groupNumber, out var storedIds);
            var liveUnits = (storedIds ?? [])
                .Select(State.UnitById)
                .Where(unit => unit is not null && unit.Owner == ProceduralRts.Core.Owner.Player && unit.Hp > 0)
                .Select(unit => unit!)
                .ToList();
            var liveIds = liveUnits.Select(unit => unit.Id).ToHashSet();
            var active = liveIds.Count > 0 && liveIds.SetEquals(selectedIds);

            snapshots.Add(new ControlGroupSnapshot(
                groupNumber,
                liveUnits.Count(IsCombatInfantryUnit),
                liveUnits.Count(IsCombatVehicleUnit),
                liveUnits.Count(IsHarvestEconomyUnit),
                active,
                _feedbackPulses[groupNumber]));
        }

        return snapshots;
    }

    private bool UseUnitBattlefieldGroups()
    {
        return UnitBattlefield is not null && UnitBattlefield.Units.Count > 0;
    }

    private IEnumerable<int> SelectedUnitIds()
    {
        return UseUnitBattlefieldGroups()
            ? UnitBattlefield!.SelectedUnits(LocalPlayerSlotId).Select(unit => unit.Id)
            : State.SelectedUnitIds();
    }

    private int SelectUnitsByIds(IEnumerable<int> unitIds)
    {
        if (!UseUnitBattlefieldGroups())
        {
            return State.SelectUnitsByIds(unitIds);
        }

        State.ClearSelection();
        return UnitBattlefield!.SelectUnitsByIds(LocalPlayerSlotId, unitIds).Count;
    }

    private Vector2? GroupCenter(IEnumerable<int> unitIds)
    {
        var requestedIds = unitIds.ToHashSet();
        var positions = UseUnitBattlefieldGroups()
            ? UnitBattlefield!.Units
                .Where(unit => unit.PlayerSlotId == LocalPlayerSlotId
                    && unit.Hp > 0
                    && requestedIds.Contains(unit.Id))
                .Select(unit => unit.Position)
                .ToList()
            : requestedIds
                .Select(State.UnitById)
                .Where(unit => unit is not null && unit.Owner == ProceduralRts.Core.Owner.Player && unit.Hp > 0)
                .Select(unit => unit!.Position)
                .ToList();

        if (positions.Count == 0)
        {
            return null;
        }

        var sum = Vector2.Zero;
        foreach (var position in positions)
        {
            sum += position;
        }

        return sum / positions.Count;
    }

    private bool IsDoubleTapRecall(int groupNumber)
    {
        return _lastRecalledGroup == groupNumber
            && CurrentSeconds() - _lastRecallSeconds <= RecallCenterDoubleTapSeconds;
    }

    private void RememberRecall(int groupNumber)
    {
        _lastRecalledGroup = groupNumber;
        _lastRecallSeconds = CurrentSeconds();
    }

    private static double CurrentSeconds()
    {
        return Time.GetTicksMsec() / 1000.0;
    }

    private IReadOnlyList<ControlGroupSnapshot> UnitBattlefieldSnapshots()
    {
        var selectedIds = UnitBattlefield!.SelectedUnits(LocalPlayerSlotId)
            .Select(unit => unit.Id)
            .ToHashSet();
        var snapshots = new List<ControlGroupSnapshot>(9);

        for (var groupNumber = 1; groupNumber <= 9; groupNumber++)
        {
            _groups.TryGetValue(groupNumber, out var storedIds);
            var requestedIds = (storedIds ?? []).ToHashSet();
            var liveUnits = UnitBattlefield.Units
                .Where(unit => unit.PlayerSlotId == LocalPlayerSlotId
                    && unit.Hp > 0
                    && requestedIds.Contains(unit.Id))
                .ToList();
            var liveIds = liveUnits.Select(unit => unit.Id).ToHashSet();
            var active = liveIds.Count > 0 && liveIds.SetEquals(selectedIds);
            var economyCount = liveUnits.Count(IsHarvestEconomyUnit);

            snapshots.Add(new ControlGroupSnapshot(
                groupNumber,
                liveUnits.Count(unit => unit.Spec.RoleTags.Contains(UnitRoleTag.Infantry)
                    && !IsHarvestEconomyUnit(unit)),
                liveUnits.Count(unit => unit.Spec.RoleTags.Contains(UnitRoleTag.Vehicle)
                    && !IsHarvestEconomyUnit(unit)),
                economyCount,
                active,
                _feedbackPulses[groupNumber]));
        }

        return snapshots;
    }

    private static int? NumberFromKey(Key key)
    {
        return key switch
        {
            Key.Key1 or Key.Kp1 => 1,
            Key.Key2 or Key.Kp2 => 2,
            Key.Key3 or Key.Kp3 => 3,
            Key.Key4 or Key.Kp4 => 4,
            Key.Key5 or Key.Kp5 => 5,
            Key.Key6 or Key.Kp6 => 6,
            Key.Key7 or Key.Kp7 => 7,
            Key.Key8 or Key.Kp8 => 8,
            Key.Key9 or Key.Kp9 => 9,
            _ => null,
        };
    }

    private static bool IsHarvestEconomyUnit(UnitInstance unit)
    {
        return IsHarvestEconomySpec(unit.Spec);
    }

    private static bool IsHarvestEconomyUnit(UnitModel unit)
    {
        return IsHarvestEconomySpec(unit.Spec);
    }

    private static bool IsCombatInfantryUnit(UnitModel unit)
    {
        var spec = unit.Spec;
        return spec.RoleTags.Contains(UnitRoleTag.Infantry)
            && !IsHarvestEconomySpec(spec);
    }

    private static bool IsCombatVehicleUnit(UnitModel unit)
    {
        var spec = unit.Spec;
        return spec.RoleTags.Contains(UnitRoleTag.Vehicle)
            && !IsHarvestEconomySpec(spec);
    }

    private static bool IsHarvestEconomySpec(UnitSpec spec)
    {
        return spec.Abilities.Any(ability => ability.Kind == AbilityKind.Harvest)
            && (spec.RoleTags.Contains(UnitRoleTag.Economy)
                || spec.RoleTags.Contains(UnitRoleTag.Worker));
    }
}
