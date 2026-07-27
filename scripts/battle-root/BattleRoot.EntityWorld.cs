using Godot;
using ProceduralRts.Controllers;
using ProceduralRts.Core;
using ProceduralRts.Ui;
using ProceduralRts.World;

namespace ProceduralRts;

public partial class BattleRoot
{
    private void ConfigureEntityWorld()
    {
        SyncEntityWorldResourceAtmosphere(_presentationEnvironment.VisualTheme);
        _unitBattlefield.EntityWorld.WorldWidth = _worldSize.X;
        _unitBattlefield.EntityWorld.WorldHeight = _worldSize.Y;
        _unitBattlefield.EntityWorld.InstallMapEnvironment(_unitBattlefield.EntityWorld.MapEnvironment);

        // Runtime hostility is owner-based, never faction-based. Mirror the
        // skirmish relation (player vs enemy) into the single live entity world.
        _unitBattlefield.EntityWorld.Relations.Set(
            OwnerId.FromPlayerSlot(PlayerSlotId.One),
            OwnerId.FromPlayerSlot(PlayerSlotId.Two),
            PlayerRelation.Hostile);
    }

    private void SyncEntityWorldResourceAtmosphere(WorldVisualThemeState _)
    {
        var atmosphere = _presentationEnvironment.ResourceAtmosphere;
        _unitBattlefield.EntityWorld.ResourceAtmosphere = atmosphere;
    }

    private void ConfigureUnitBattlefield()
    {
        _unitBattlefield.WorldSize = _worldSize;
        _unitBattlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Hostile);

        if (_matchConfig.LaunchMode == LaunchMode.Sandbox)
        {
            _unitBattlefield.SpawnRoster(UnitRosters.DogT1, PlayerSlotId.One, new Vector2(820, 1180), new Vector2(58, 0));
            _unitBattlefield.SpawnRoster(UnitRosters.DogT1, PlayerSlotId.Two, new Vector2(1280, 1180), new Vector2(58, 0), Mathf.Pi);
        }

        foreach (var unit in _unitBattlefield.Units)
        {
            AddUnitInstanceView(unit);
        }
    }

    private void SpawnStartingUnitDesigns(PlayerSlotId playerSlotId, UnitFactionId faction, Vector2 origin, float facing)
    {
        foreach (var spawn in UnitDesignRuntimeLoadouts.StartingUnits(faction))
        {
            _unitBattlefield.Spawn(spawn.DesignId, playerSlotId, origin + spawn.Offset.Rotated(facing), facing + spawn.FacingOffset);
        }
    }

    private IReadOnlyList<UnitInstance> DebugSpawnActiveBattlePerfUnits(
        PlayerSlotId playerSlotId,
        UnitFactionId faction,
        Vector2 center,
        float facing,
        int count)
    {
        var designIds = ActiveBattlePerfDesignsFor(faction);
        var units = new List<UnitInstance>(count);
        const int columns = 6;
        const float spacing = 54;
        var rows = (int)MathF.Ceiling(count / (float)columns);
        for (var index = 0; index < count; index++)
        {
            var column = index % columns;
            var row = index / columns;
            var offset = new Vector2(
                (column - (columns - 1) * 0.5f) * spacing,
                (row - (rows - 1) * 0.5f) * spacing).Rotated(facing);
            var unit = _unitBattlefield.Spawn(
                designIds[index % designIds.Count],
                playerSlotId,
                center + offset,
                facing);
            AddUnitInstanceView(unit);
            units.Add(unit);
        }

        return units;
    }

    private static IReadOnlyList<string> ActiveBattlePerfDesignsFor(UnitFactionId faction)
    {
        return faction == UnitFactionId.Cat ? CatActiveBattlePerfDesigns : DogActiveBattlePerfDesigns;
    }

    private void AddUnitInstanceView(UnitInstance unit)
    {
        if (_unitInstanceViews.ContainsKey(unit.Id))
        {
            return;
        }

        var view = new UnitInstanceView
        {
            Name = $"UnitInstance_{unit.Id}",
            Unit = unit,
            Viewer = PlayerSlotId.One,
            Relations = _unitBattlefield.Relations,
            PresentationProvider = () => _unitBattlefield.UnitPresentationProjection(unit.Id),
            VisualThemeProvider = () => _presentationEnvironment.VisualTheme,
        };
        _unitInstanceRoot.AddChild(view);
        _unitInstanceViews[unit.Id] = view;
    }

    private static UnitFactionId ToUnitFaction(FactionId factionId)
    {
        return factionId switch
        {
            FactionId.Dog => UnitFactionId.Dog,
            FactionId.Cat => UnitFactionId.Cat,
            _ => UnitFactionId.Dog,
        };
    }

    private static FactionId ToFactionId(UnitFactionId factionId)
    {
        return factionId switch
        {
            UnitFactionId.Dog => FactionId.Dog,
            UnitFactionId.Cat => FactionId.Cat,
            UnitFactionId.Corruption => FactionId.Corruption,
            _ => FactionId.Dog,
        };
    }

}
