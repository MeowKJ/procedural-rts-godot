using Godot;
using ProceduralRts.Core;
using ProceduralRts.MapAuthoring.Editor;
using ProceduralRts.MapAuthoring.Nodes;

namespace ProceduralRts.MapAuthoring.Qa;

static class MapCatalogPropertyScenarios
{
    private const string UnknownId = "unknown.visual-sentinel.building";

    public static void Run(Building building)
    {
        var propertyName = building.GetPropertyList()
            .Select(property => property["name"].AsStringName().ToString())
            .Single(name => name == "BuildingId" || name == "BuildingId".ToSnakeCase());
        var property = new MapCatalogOptionProperty();
        try
        {
            property.SetObjectAndProperty(building, propertyName);
            property._UpdateProperty();
            Require(property.CurrentOptions.SequenceEqual(MapAuthoringCatalog.BuildingIds),
                "Parameterless catalog property must repopulate known Building options.");
            Require(property.CurrentText == building.BuildingId, "Known Building id must display without decoration.");

            var original = building.BuildingId;
            var changeCount = 0;
            property.Connect(EditorProperty.SignalName.PropertyChanged,
                Callable.From<StringName, Variant, StringName, bool>((changedProperty, value, _, _) =>
                {
                    changeCount++;
                    building.Set(changedProperty, value);
                }));
            var survivingControl = property.GetNode<OptionButton>("CatalogOptions");
            property._UpdateProperty();
            property._UpdateProperty();
            Require(property.GetNode<OptionButton>("CatalogOptions").GetInstanceId() == survivingControl.GetInstanceId(),
                "Catalog property must reuse its surviving named control.");
            Require(survivingControl.GetSignalConnectionList(OptionButton.SignalName.ItemSelected).Count == 1,
                "Catalog property reuse must retain exactly one ItemSelected handler.");
            var selectedId = MapAuthoringCatalog.BuildingIds.First(id => id != original);
            var selectedIndex = Enumerable.Range(0, MapAuthoringCatalog.BuildingIds.Count)
                .Single(index => MapAuthoringCatalog.BuildingIds[index] == selectedId);
            survivingControl.EmitSignal(OptionButton.SignalName.ItemSelected, selectedIndex);
            Require(changeCount == 1 && building.BuildingId == selectedId,
                "Reused catalog control selection must emit once and persist the selected stable id.");
            building.BuildingId = original;
            property._UpdateProperty();

            var staleControl = property.GetNode<OptionButton>("CatalogOptions");
            property.RemoveChild(staleControl);
            staleControl.Free();
            property._UpdateProperty();
            Require(property.CurrentOptions.SequenceEqual(MapAuthoringCatalog.BuildingIds),
                "Catalog property must recreate its control and repopulate known options after a reload-style detach.");

            building.BuildingId = UnknownId;
            property._UpdateProperty();
            Require(property.CurrentOptions.SequenceEqual(MapAuthoringCatalog.BuildingIds),
                "Unknown value must not erase the authoritative option list.");
            Require(property.CurrentText == $"Unknown: {UnknownId}", "Unknown persisted id must remain visible without mutation.");
            Require(building.BuildingId == UnknownId, "Unknown persisted id must remain unchanged by Inspector refresh.");
            building.BuildingId = original;
        }
        finally
        {
            property.Free();
        }

        RunQuarterTurnReload(building);
    }

    private static void RunQuarterTurnReload(Building building)
    {
        var rotationName = building.GetPropertyList()
            .Select(property => property["name"].AsStringName().ToString())
            .Single(name => MapAuthoringInspectorCatalog.IsBuildingRotation(building, name));
        var property = new MapQuarterTurnProperty();
        try
        {
            property.SetObjectAndProperty(building, rotationName);
            property._UpdateProperty();
            var expected = MapBuildingQuarterTurns.All.Select(turn => turn.Label);
            Require(property.CurrentOptions.SequenceEqual(expected), "Quarter-turn property must expose four stable options.");

            var original = building.Rotation;
            var changeCount = 0;
            property.Connect(EditorProperty.SignalName.PropertyChanged,
                Callable.From<StringName, Variant, StringName, bool>((changedProperty, value, _, _) =>
                {
                    changeCount++;
                    building.Set(changedProperty, value);
                }));
            var survivingControl = property.GetNode<OptionButton>("QuarterTurns");
            property._UpdateProperty();
            property._UpdateProperty();
            Require(property.GetNode<OptionButton>("QuarterTurns").GetInstanceId() == survivingControl.GetInstanceId(),
                "Quarter-turn property must reuse its surviving named control.");
            Require(survivingControl.GetSignalConnectionList(OptionButton.SignalName.ItemSelected).Count == 1,
                "Quarter-turn property reuse must retain exactly one ItemSelected handler.");
            survivingControl.EmitSignal(OptionButton.SignalName.ItemSelected, 1);
            Require(changeCount == 1 && Mathf.IsEqualApprox(building.Rotation, MapBuildingQuarterTurns.All[1].Radians),
                "Reused quarter-turn control selection must emit once and persist the selected rotation.");
            building.Rotation = original;
            property._UpdateProperty();

            var staleControl = property.GetNode<OptionButton>("QuarterTurns");
            property.RemoveChild(staleControl);
            staleControl.Free();
            property._UpdateProperty();
            Require(property.CurrentOptions.SequenceEqual(expected),
                "Quarter-turn property must recreate and repopulate its control after a reload-style detach.");
        }
        finally
        {
            property.Free();
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
