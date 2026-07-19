namespace ProceduralRts.Core;

public enum UnitStanceStripSelectionState
{
    None,
    Mixed,
    Uniform,
}

public readonly record struct UnitStanceStripProjection
{
    private UnitStanceStripProjection(
        UnitStanceStripSelectionState state,
        UnitStance? selectedStance,
        int selectedUnitCount)
    {
        State = state;
        SelectedStance = selectedStance;
        SelectedUnitCount = selectedUnitCount;
    }

    public UnitStanceStripSelectionState State { get; }
    public UnitStance? SelectedStance { get; }
    public int SelectedUnitCount { get; }

    public static UnitStanceStripProjection None { get; } = new(
        UnitStanceStripSelectionState.None,
        selectedStance: null,
        selectedUnitCount: 0);

    public static UnitStanceStripProjection FromSelection(UnitStance? selectedStance, int selectedUnitCount)
    {
        var count = Math.Max(0, selectedUnitCount);
        if (count == 0)
        {
            return None;
        }

        return selectedStance is { } stance
            ? new UnitStanceStripProjection(UnitStanceStripSelectionState.Uniform, stance, count)
            : new UnitStanceStripProjection(UnitStanceStripSelectionState.Mixed, selectedStance: null, count);
    }

    public bool IsSelected(UnitStance stance)
    {
        return State == UnitStanceStripSelectionState.Uniform && SelectedStance == stance;
    }
}
