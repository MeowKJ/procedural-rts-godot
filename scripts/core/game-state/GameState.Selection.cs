using Godot;

namespace ProceduralRts.Core;

public sealed partial class GameState
{
    public IEnumerable<UnitModel> SelectedUnits()
    {
        return Units.Where(unit => unit.Owner == Owner.Player && unit.Selected);
    }

    public IEnumerable<BuildingModel> SelectedBuildings()
    {
        return Buildings.Where(building => building.Owner == Owner.Player && building.Selected);
    }

    public int SelectedCount()
    {
        return SelectedUnits().Count() + SelectedBuildings().Count();
    }

    public IReadOnlyList<int> SelectedUnitIds()
    {
        return SelectedUnits().Select(unit => unit.Id).ToList();
    }

    public void ClearSelection()
    {
        foreach (var unit in Units)
        {
            unit.Selected = false;
        }

        foreach (var building in Buildings)
        {
            building.Selected = false;
        }
    }

    public int SelectSingleAt(Vector2 worldPoint, bool additive, float pickPadding = 8)
    {
        var hit = PickUnit(worldPoint, Owner.Player, pickPadding);
        if (!additive)
        {
            ClearSelection();
        }

        if (hit is not null)
        {
            hit.Selected = additive ? !hit.Selected : true;
            return SelectedCount();
        }

        var buildingHit = PickBuilding(worldPoint, owner => owner == Owner.Player, pickPadding);
        if (buildingHit is not null)
        {
            buildingHit.Selected = additive ? !buildingHit.Selected : true;
        }

        return SelectedCount();
    }

    public int SelectPlayerBuildingAt(Vector2 worldPoint, bool additive, float pickPadding = 8)
    {
        if (!additive)
        {
            ClearSelection();
        }

        var buildingHit = PickBuilding(worldPoint, owner => owner == Owner.Player, pickPadding);
        if (buildingHit is not null)
        {
            buildingHit.Selected = additive ? !buildingHit.Selected : true;
        }

        return SelectedCount();
    }

    public int SelectSameUnitsAt(Vector2 worldPoint, Rect2 visibleWorldRect, bool additive, float pickPadding = 8)
    {
        var hit = PickUnit(worldPoint, Owner.Player, pickPadding);
        if (hit is null)
        {
            return SelectSingleAt(worldPoint, additive, pickPadding);
        }

        if (!additive)
        {
            ClearSelection();
        }
        else
        {
            foreach (var building in Buildings)
            {
                building.Selected = false;
            }
        }

        foreach (var unit in Units.Where(unit => unit.Owner == Owner.Player && unit.DesignId == hit.DesignId))
        {
            unit.Selected = visibleWorldRect.HasPoint(unit.Position) || (additive && unit.Selected);
        }

        return SelectedUnits().Count();
    }

    public int SelectUnitsByIds(IEnumerable<int> unitIds)
    {
        var requestedIds = unitIds.ToHashSet();
        var selectedCount = 0;

        foreach (var unit in Units)
        {
            unit.Selected = unit.Owner == Owner.Player && requestedIds.Contains(unit.Id);
            if (unit.Selected)
            {
                selectedCount++;
            }
        }

        foreach (var building in Buildings)
        {
            building.Selected = false;
        }

        return selectedCount;
    }

    public int SelectRect(Rect2 worldRect, bool additive)
    {
        if (!additive)
        {
            ClearSelection();
        }
        else
        {
            foreach (var building in Buildings)
            {
                building.Selected = false;
            }
        }

        var normalizedRect = NormalizedSelectionRect(worldRect);
        var unitsInRect = Units
            .Where(unit => unit.Owner == Owner.Player && UnitOverlapsSelectionRect(normalizedRect, unit))
            .ToList();
        var harvestersInRect = unitsInRect
            .Where(IsHarvesterUnit)
            .ToList();
        var combatUnitsInRect = unitsInRect
            .Where(unit => !IsHarvesterUnit(unit))
            .ToList();
        var includeHarvesters = ShouldIncludeHarvestersInSelectionRect(normalizedRect, harvestersInRect, combatUnitsInRect);

        foreach (var unit in Units.Where(unit => unit.Owner == Owner.Player))
        {
            var selectableByBox = UnitOverlapsSelectionRect(normalizedRect, unit)
                && (!IsHarvesterUnit(unit) || includeHarvesters);
            unit.Selected = selectableByBox || (additive && unit.Selected);
        }

        return SelectedUnits().Count();
    }

    private bool UnitOverlapsSelectionRect(Rect2 worldRect, UnitModel unit)
    {
        if (worldRect.HasPoint(unit.Position))
        {
            return true;
        }

        var radius = unit.RuntimeDescriptor.Radius * 0.72f;
        var closest = new Vector2(
            Mathf.Clamp(unit.Position.X, worldRect.Position.X, worldRect.End.X),
            Mathf.Clamp(unit.Position.Y, worldRect.Position.Y, worldRect.End.Y));
        return closest.DistanceSquaredTo(unit.Position) <= radius * radius;
    }

    private static Rect2 NormalizedSelectionRect(Rect2 rect)
    {
        var minX = Mathf.Min(rect.Position.X, rect.End.X);
        var minY = Mathf.Min(rect.Position.Y, rect.End.Y);
        var maxX = Mathf.Max(rect.Position.X, rect.End.X);
        var maxY = Mathf.Max(rect.Position.Y, rect.End.Y);
        return new Rect2(minX, minY, maxX - minX, maxY - minY);
    }

    private static bool ShouldIncludeHarvestersInSelectionRect(
        Rect2 worldRect,
        IReadOnlyList<UnitModel> harvestersInRect,
        IReadOnlyList<UnitModel> combatUnitsInRect)
    {
        if (harvestersInRect.Count == 0)
        {
            return false;
        }

        if (combatUnitsInRect.Count == 0)
        {
            return true;
        }

        if (harvestersInRect.Count > combatUnitsInRect.Count)
        {
            return true;
        }

        var rectSize = worldRect.Size;
        var maxSide = Mathf.Max(Mathf.Abs(rectSize.X), Mathf.Abs(rectSize.Y));
        if (maxSide > HarvesterBoxIntentMaxSize)
        {
            return false;
        }

        var center = worldRect.Position + worldRect.Size / 2f;
        var nearestHarvester = harvestersInRect.Min(unit => unit.Position.DistanceTo(center));
        var nearestCombat = combatUnitsInRect.Min(unit => unit.Position.DistanceTo(center));
        return nearestHarvester <= nearestCombat + HarvesterBoxIntentCenterMargin;
    }
}
