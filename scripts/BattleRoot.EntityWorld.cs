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
        if (!RunEntityWorldShadow)
        {
            return;
        }

        SimSystemPipeline.ConfigureLiveGameplay(
            _entityWorld,
            OwnerId.FromPlayerSlot(PlayerSlotId.One));

        // Runtime hostility is owner-based, never faction-based. Mirror the
        // skirmish relation (player vs enemy) into the entity world.
        _entityWorld.Relations.Set(
            OwnerId.FromPlayerSlot(PlayerSlotId.One),
            OwnerId.FromPlayerSlot(PlayerSlotId.Two),
            PlayerRelation.Hostile);
    }

    private void StepEntityWorld(double delta)
    {
        if (!RunEntityWorldShadow)
        {
            return;
        }

        // Convert variable frame delta into whole fixed ticks and step the
        // authoritative world once per tick, draining due commands in stable
        // order. Keeps the new sim path running on a deterministic clock.
        var ticks = _simClock.Advance(delta);
        _entityWorld.Metrics.RecordClockBacklogDrop(_simClock.LastDroppedBacklogTicks, _simClock.LastDroppedBacklogSeconds);
        for (var i = 0; i < ticks; i++)
        {
            var tick = _simClock.CurrentTick - ticks + 1 + i;
            var due = _entityCommands.DrainUpToTick(tick);
            _entityWorld.Step(tick, _simClock.FixedDelta, due);
            // Feed quality metrics from the event stream (read-only; the live view
            // path will later consume these same events for effects/audio/HUD).
            _entityWorld.Events.DrainInto(_simEventDrainBuffer);
            _entityWorld.Metrics.Consume(_simEventDrainBuffer);
            _simEventDrainBuffer.Clear();
        }
    }

    private void ConfigureUnitBattlefield()
    {
        _unitBattlefield.WorldSize = _state.WorldSize;
        _unitBattlefield.SetResourceFields(_state.ResourceFields);
        _unitBattlefield.SetCredits(PlayerSlotId.One, _state.Credits(ProceduralRts.Core.Owner.Player));
        _unitBattlefield.SetCredits(PlayerSlotId.Two, _state.Credits(ProceduralRts.Core.Owner.Enemy));
        _unitBattlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Hostile);

        if (_state.Options.LaunchMode == LaunchMode.Sandbox)
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
            ProjectionProvider = () => _unitBattlefield.UnitProjection(unit.Id),
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
