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
        SyncEntityWorldResourceAtmosphere(_state.VisualTheme);
        _unitBattlefield.EntityWorld.WorldWidth = _state.WorldSize.X;
        _unitBattlefield.EntityWorld.WorldHeight = _state.WorldSize.Y;
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
        var atmosphere = _state.ResourceAtmosphere;
        _unitBattlefield.EntityWorld.ResourceAtmosphere = atmosphere;
    }

    private void ConfigureUnitBattlefield()
    {
        _unitBattlefield.WorldSize = _state.WorldSize;
        _unitBattlefield.SetResourceFields(_state.ResourceFields);
        _unitBattlefield.SetCredits(PlayerSlotId.One, _state.Credits(ProceduralRts.Core.Owner.Player));
        _unitBattlefield.SetCredits(PlayerSlotId.Two, _state.Credits(ProceduralRts.Core.Owner.Enemy));
        _unitBattlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Hostile);

        if (_state.MatchConfig.AuthoredMap is not null)
        {
            // MapLoader already populated the authoritative EntityWorld and
            // UnitBattlefield adopted those exact entities before the scene ran.
        }
        else if (_state.Options.LaunchMode == LaunchMode.Sandbox)
        {
            _unitBattlefield.SpawnRoster(UnitRosters.DogT1, PlayerSlotId.One, new Vector2(820, 1180), new Vector2(58, 0));
            _unitBattlefield.SpawnRoster(UnitRosters.DogT1, PlayerSlotId.Two, new Vector2(1280, 1180), new Vector2(58, 0));
        }
        else
        {
            SpawnStartingUnitDesigns(PlayerSlotId.One, ToUnitFaction(_state.Options.PlayerFaction), new Vector2(720, 760), 0);
            SpawnStartingUnitDesigns(PlayerSlotId.Two, ToUnitFaction(_state.Options.AiFaction), new Vector2(2510, 1370), Mathf.Pi);
        }

        foreach (var unit in _unitBattlefield.Units)
        {
            if (_state.Options.LaunchMode == LaunchMode.Sandbox)
            {
                SetUnitInstanceFacing(unit, unit.PlayerSlotId == PlayerSlotId.One ? 0 : Mathf.Pi);
            }

            AddUnitInstanceView(unit);
        }
    }

    private void SpawnStartingUnitDesigns(PlayerSlotId playerSlotId, UnitFactionId faction, Vector2 origin, float facing)
    {
        foreach (var spawn in UnitDesignRuntimeLoadouts.StartingUnits(faction))
        {
            var unit = _unitBattlefield.Spawn(spawn.DesignId, playerSlotId, origin + spawn.Offset.Rotated(facing), facing + spawn.FacingOffset);
            SetUnitInstanceFacing(unit, facing + spawn.FacingOffset);
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
            SetUnitInstanceFacing(unit, facing);
            AddUnitInstanceView(unit);
            units.Add(unit);
        }

        return units;
    }

    private static IReadOnlyList<string> ActiveBattlePerfDesignsFor(UnitFactionId faction)
    {
        return faction == UnitFactionId.Cat ? CatActiveBattlePerfDesigns : DogActiveBattlePerfDesigns;
    }

    private static void SetUnitInstanceFacing(UnitInstance unit, float facing)
    {
        unit.Facing = facing;
        for (var index = 0; index < unit.WeaponMounts.Count; index++)
        {
            unit.WeaponMounts[index] = unit.WeaponMounts[index] with { Facing = facing };
        }
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
            VisualThemeProvider = () => _state.VisualTheme,
            DrawBodyArt = false,
        };
        _unitInstanceRoot.AddChild(view);
        _unitInstanceViews[unit.Id] = view;
    }

    private void UpsertBuildingTarget(BuildingModel building)
    {
        _unitBattlefield.UpsertBuildingTarget(
            building.Id,
            building.Kind,
            ToPlayerSlot(building.Owner),
            ToUnitFaction(building.FactionId),
            building.Position,
            building.Facing,
            building.Hp,
            building.Powered,
            building.BuildProgress,
            building.RallyPoint);
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

    private static FactionId ToLegacyFaction(UnitFactionId factionId)
    {
        return factionId switch
        {
            UnitFactionId.Dog => FactionId.Dog,
            UnitFactionId.Cat => FactionId.Cat,
            UnitFactionId.Corruption => FactionId.Corruption,
            _ => FactionId.Dog,
        };
    }

    private static PlayerSlotId ToPlayerSlot(ProceduralRts.Core.Owner owner)
    {
        return owner == ProceduralRts.Core.Owner.Player ? PlayerSlotId.One : PlayerSlotId.Two;
    }

}
