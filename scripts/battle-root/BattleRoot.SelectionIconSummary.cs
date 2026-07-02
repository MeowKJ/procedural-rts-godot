using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts;

public partial class BattleRoot
{
    private readonly List<SelectionIconSummaryEntry> _selectionIconSummaryEntries = [];
    private readonly List<HudLayer.SelectionIconItem> _selectionIconSummaryBuffer = [];
    private readonly List<HudLayer.SelectionIconItem> _selectionIconSummarySecondaryBuffer = [];
    private bool _useSecondarySelectionIconSummaryBuffer;

    private IReadOnlyList<HudLayer.SelectionIconItem> SelectionIconSummary(
        IReadOnlyList<UnitModel> units,
        IReadOnlyList<BuildingModel> buildings)
    {
        var result = NextSelectionIconSummaryBuffer();
        _selectionIconSummaryEntries.Clear();
        foreach (var unit in units)
        {
            AddLegacyUnitIconSummaryEntry(unit);
        }

        WriteSelectionIconSummary(result);
        AppendLegacyBuildingIconSummary(buildings, result);
        return result;
    }

    private IReadOnlyList<HudLayer.SelectionIconItem> UnitBattlefieldBuildingIconSummary(IReadOnlyList<BuildingSelectionProjection> buildings)
    {
        var result = NextSelectionIconSummaryBuffer();
        _selectionIconSummaryEntries.Clear();
        foreach (var building in buildings)
        {
            AddRuntimeBuildingIconSummaryEntry(building);
        }

        WriteSelectionIconSummary(result);
        return result;
    }

    private IReadOnlyList<HudLayer.SelectionIconItem> UnitInstanceIconSummary(IReadOnlyList<UnitInstance> units)
    {
        var result = NextSelectionIconSummaryBuffer();
        _selectionIconSummaryEntries.Clear();
        foreach (var unit in units)
        {
            AddUnitInstanceIconSummaryEntry(unit);
        }

        WriteSelectionIconSummary(result);
        return result;
    }

    private List<HudLayer.SelectionIconItem> NextSelectionIconSummaryBuffer()
    {
        _useSecondarySelectionIconSummaryBuffer = !_useSecondarySelectionIconSummaryBuffer;
        var result = _useSecondarySelectionIconSummaryBuffer
            ? _selectionIconSummarySecondaryBuffer
            : _selectionIconSummaryBuffer;
        result.Clear();
        return result;
    }

    private void AddUnitInstanceIconSummaryEntry(UnitInstance unit)
    {
        var index = IndexOfSelectionIconSummaryEntry(0, unit.Spec.Id);
        if (index >= 0)
        {
            var entry = _selectionIconSummaryEntries[index];
            entry.Count++;
            _selectionIconSummaryEntries[index] = entry;
            return;
        }

        _selectionIconSummaryEntries.Add(new SelectionIconSummaryEntry(
            0,
            unit.Spec.Id,
            null,
            unit.Spec.Icon,
            unit.Spec.ShortCode,
            1,
            PlayerSlotAccent(unit.PlayerSlotId),
            unit.Spec.Id));
    }

    private void AddRuntimeBuildingIconSummaryEntry(BuildingSelectionProjection building)
    {
        const int sortOrdinal = 0;
        var sortKey = building.Kind;
        var index = IndexOfSelectionIconSummaryEntry(sortOrdinal, sortKey);
        if (index >= 0)
        {
            var entry = _selectionIconSummaryEntries[index];
            entry.Count++;
            _selectionIconSummaryEntries[index] = entry;
            return;
        }

        _selectionIconSummaryEntries.Add(new SelectionIconSummaryEntry(
            sortOrdinal,
            sortKey,
            null,
            building.Icon,
            building.ShortCode,
            1,
            building.Accent,
            null));
    }

    private void AddLegacyUnitIconSummaryEntry(UnitModel unit)
    {
        var index = IndexOfSelectionIconSummaryEntry(0, unit.DesignId);
        if (index >= 0)
        {
            var entry = _selectionIconSummaryEntries[index];
            entry.Count++;
            if (entry.FactionId != unit.FactionId)
            {
                entry.FactionId = null;
            }

            _selectionIconSummaryEntries[index] = entry;
            return;
        }

        var style = UnitSpecReadPathFor(unit);
        _selectionIconSummaryEntries.Add(new SelectionIconSummaryEntry(
            0,
            unit.DesignId,
            unit.FactionId,
            style.Presentation.Icon,
            style.Presentation.ShortCode,
            1,
            style.EntityAccent,
            style.Spec.Id));
    }

    private void AppendLegacyBuildingIconSummary(
        IReadOnlyList<BuildingModel> buildings,
        List<HudLayer.SelectionIconItem> result)
    {
        if (buildings.Count == 0)
        {
            return;
        }

        var sample = buildings[0];
        FactionId? factionId = sample.FactionId;
        for (var i = 1; i < buildings.Count; i++)
        {
            if (buildings[i].FactionId != factionId)
            {
                factionId = null;
                break;
            }
        }

        var spec = BuildSpecCatalog.For(sample.Kind);
        var entityAccent = _state.VisualAccent(sample.Owner, sample.FactionId, spec.Accent);
        result.Add(new HudLayer.SelectionIconItem(
            factionId,
            spec.Icon,
            spec.ShortCode,
            buildings.Count,
            entityAccent));
    }

    private int IndexOfSelectionIconSummaryEntry(int sortOrdinal, string sortKey)
    {
        for (var i = 0; i < _selectionIconSummaryEntries.Count; i++)
        {
            var entry = _selectionIconSummaryEntries[i];
            if (entry.SortOrdinal == sortOrdinal && entry.SortKey == sortKey)
            {
                return i;
            }
        }

        return -1;
    }

    private void WriteSelectionIconSummary(List<HudLayer.SelectionIconItem> result)
    {
        _selectionIconSummaryEntries.Sort(CompareSelectionIconSummaryEntries);
        foreach (var entry in _selectionIconSummaryEntries)
        {
            result.Add(new HudLayer.SelectionIconItem(
                entry.FactionId,
                entry.Glyph,
                entry.Label,
                entry.Count,
                entry.Accent,
                entry.UnitDesignId));
        }
    }

    private static int CompareSelectionIconSummaryEntries(SelectionIconSummaryEntry left, SelectionIconSummaryEntry right)
    {
        var count = right.Count.CompareTo(left.Count);
        if (count != 0)
        {
            return count;
        }

        var ordinal = left.SortOrdinal.CompareTo(right.SortOrdinal);
        return ordinal != 0
            ? ordinal
            : string.Compare(left.SortKey, right.SortKey, StringComparison.Ordinal);
    }

    private record struct SelectionIconSummaryEntry(
        int SortOrdinal,
        string SortKey,
        FactionId? FactionId,
        IconGlyph Glyph,
        string Label,
        int Count,
        Color Accent,
        string? UnitDesignId);
}
