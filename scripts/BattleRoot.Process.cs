using Godot;
using ProceduralRts.Controllers;
using ProceduralRts.Core;
using ProceduralRts.Ui;
using ProceduralRts.World;

namespace ProceduralRts;

public partial class BattleRoot
{
    private readonly List<(Vector2 Position, float SightRange)> _unitBattlefieldVisionSourceBuffer = [];

    public override void _Process(double delta)
    {
        _processStopwatch.Restart();
        _elapsed += (float)delta;
        var gameplayDelta = SandboxTimeScaleMath.ScaledGameplayDelta(delta, _state.Options.LaunchMode, _sandboxTimeScale);
        if (UseUnitDesignRuntime)
        {
            _state.UpdateWorldOnly(gameplayDelta, UnitBattlefieldVisionSources());
        }
        else
        {
            _state.Update(gameplayDelta);
        }

        _unitBattlefield.Update(gameplayDelta);
        _simStepStopwatch.Restart();
        StepEntityWorld(gameplayDelta);
        _simStepStopwatch.Stop();
        SyncUnitBattlefieldBuildingRuntimeState();
        RefreshSelectionInfo();
        RefreshCommandCard();
        _minimapRefreshTimer -= (float)delta;
        if (_minimapRefreshTimer <= 0)
        {
            _minimapRefreshTimer = MinimapRefreshInterval;
            RefreshMinimap();
        }

        RefreshControlGroups();
        UpdateIdleHarvesterAlert();
        UpdatePowerAlert(false);
        RefreshAlerts((float)delta);
        RefreshCommandPreview();
        _viewCullingTimer -= (float)delta;
        if (_viewCullingTimer <= 0)
        {
            _viewCullingTimer = ViewCullingInterval;
            RefreshViewCulling();
        }

        _processStopwatch.Stop();
        _presentationMetrics.RecordFrame(
            delta * 1000.0,
            _processStopwatch.Elapsed.TotalMilliseconds,
            _simStepStopwatch.Elapsed.TotalMilliseconds);
    }

    private void RefreshViewCulling()
    {
        if (_camera is null)
        {
            return;
        }

        _viewCullingTimer = ViewCullingInterval;
        var visibleRect = _camera.VisibleWorldRect().Grow(ViewCullingMargin);
        _grid.VisibleWorldRect = visibleRect;
        _combatEffects.CullingWorldRect = visibleRect;
        _commandAcknowledgements.CullingWorldRect = visibleRect;
        _footprints.CullingWorldRect = visibleRect;
        _fogOfWar.VisibleWorldRect = visibleRect;

        foreach (var (id, view) in _buildingViews)
        {
            var projection = UseUnitDesignRuntime
                ? _unitBattlefield.BuildingPresentationProjection(id)
                : null;
            var shouldShow = projection is { } liveBuilding
                ? liveBuilding.Entity.IsAlive && visibleRect.Intersects(BuildingProjectionWorldRect(liveBuilding))
                : view.Building.Hp > 0 && visibleRect.Intersects(BuildingWorldRect(view.Building));
            if (shouldShow)
            {
                view.Position = projection?.Entity.Position ?? view.Building.Position;
                view.Rotation = projection?.Entity.Facing ?? view.Building.Facing;
            }

            SetPresentationViewActive(view, shouldShow);
        }

        foreach (var (id, view) in _unitViews)
        {
            var unit = view.Unit;
            var radius = UnitSpecReadPathFor(unit).Descriptor.Radius;
            var unitRect = new Rect2(unit.Position - Vector2.One * radius, Vector2.One * radius * 2f);
            var shouldShow = unit.Hp > 0 && (visibleRect.HasPoint(unit.Position) || visibleRect.Intersects(unitRect));
            if (shouldShow)
            {
                view.Position = unit.Position;
            }

            SetPresentationViewActive(view, shouldShow);
        }

        foreach (var (id, view) in _unitInstanceViews)
        {
            var unit = view.Unit;
            var radius = unit.Spec.Collision.Radius;
            var shouldShow = unit.Hp > 0
                && visibleRect.Intersects(new Rect2(unit.Position - Vector2.One * radius, Vector2.One * radius * 2f));
            if (shouldShow)
            {
                view.Position = unit.Position;
            }

            SetPresentationViewActive(view, shouldShow);
        }

        foreach (var (id, view) in _resourceViews)
        {
            var field = view.Field;
            var shouldShow = field.Amount > 0
                && visibleRect.Intersects(new Rect2(field.Position - Vector2.One * field.Radius, Vector2.One * field.Radius * 2f));
            if (shouldShow)
            {
                view.Position = field.Position;
            }

            SetPresentationViewActive(view, shouldShow);
        }
    }

    private PerfHudCounts PerfHudCounts()
    {
        var liveUnitCount = UseUnitDesignRuntime
            ? LiveUnitBattlefieldUnitCount()
            : LiveLegacyUnitCount();
        var liveBuildingCount = UseUnitDesignRuntime
            ? _unitBattlefield.LiveBuildingCount()
            : LiveLegacyBuildingCount();
        var visibleUnitCount = VisibleUnitViewCount();
        var projectileCount = _state.Projectiles.Count + _state.Beams.Count;
        var effectCount = (_combatEffects?.ActiveEffectCount ?? 0)
            + (_commandAcknowledgements?.ActiveRingCount ?? 0)
            + (_footprints?.ActiveMarkCount ?? 0);

        return new PerfHudCounts(
            liveUnitCount + liveBuildingCount,
            liveUnitCount,
            visibleUnitCount,
            projectileCount,
            effectCount,
            _state.FogOfWar.MaskTextureUploadCount,
            _state.LastFogUpdateMs);
    }

    private int LiveUnitBattlefieldUnitCount()
    {
        var count = 0;
        foreach (var unit in _unitBattlefield.Units)
        {
            if (unit.Hp > 0)
            {
                count++;
            }
        }

        return count;
    }

    private int LiveLegacyUnitCount()
    {
        var count = 0;
        foreach (var unit in _state.Units)
        {
            if (unit.Hp > 0)
            {
                count++;
            }
        }

        return count;
    }

    private int LiveLegacyBuildingCount()
    {
        var count = 0;
        foreach (var building in _state.Buildings)
        {
            if (building.Hp > 0)
            {
                count++;
            }
        }

        return count;
    }

    private int VisibleUnitViewCount()
    {
        var count = 0;
        foreach (var (_, view) in _unitInstanceViews)
        {
            if (view.Visible)
            {
                count++;
            }
        }

        foreach (var (_, view) in _unitViews)
        {
            if (view.Visible)
            {
                count++;
            }
        }

        return count;
    }

    private static void SetPresentationViewActive(Node2D view, bool active)
    {
        if (view.Visible == active
            && view.ProcessMode == (active ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled))
        {
            return;
        }

        view.Visible = active;
        view.ProcessMode = active ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
        if (active)
        {
            view.QueueRedraw();
        }
    }

    private void SyncUnitBattlefieldBuildingRuntimeState()
    {
        foreach (var target in _unitBattlefield.BuildingSnapshots())
        {
            var presentation = _unitBattlefield.BuildingPresentationProjection(target.Id);
            var building = _state.UpsertRuntimeBuilding(target, presentation);
            if (!_buildingViews.ContainsKey(building.Id))
            {
                var view = CreateBuildingView(building);
                _buildingRoot.AddChild(view);
                _buildingViews[building.Id] = view;
                if (building.Owner == ProceduralRts.Core.Owner.Player)
                {
                    AddAlert(AlertKind.Building, GameText.Format("ui.building.online", BuildSpecCatalog.For(building.Kind).Label), building.Position);
                }
            }

            building.DeliveryPulse = Mathf.Max(building.DeliveryPulse, presentation?.DeliveryPulse ?? 0);
            building.DockReservedByHarvesterId = _unitBattlefield.BuildingDockReservedByHarvesterId(target.Id);
            building.DockedHarvesterId = _unitBattlefield.BuildingDockedHarvesterId(target.Id);
            building.RallyPoint = presentation?.RallyPoint;
            building.RallyPulse = Mathf.Max(building.RallyPulse, presentation?.RallyPulse ?? 0);
            building.Selected = _unitBattlefield.BuildingProjection(target.Id)?.Selected == true;
        }
    }

    private IReadOnlyList<(Vector2 Position, float SightRange)> UnitBattlefieldVisionSources()
    {
        _unitBattlefieldVisionSourceBuffer.Clear();
        foreach (var source in _unitBattlefield.VisionSources(PlayerSlotId.One))
        {
            _unitBattlefieldVisionSourceBuffer.Add((source.Position, source.SightRange));
        }

        return _unitBattlefieldVisionSourceBuffer;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_state.Options.LaunchMode != LaunchMode.Sandbox
            || @event is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return;
        }

        if (key.Keycode is Key.F2 or Key.F3 or Key.F4)
        {
            ApplySandboxTimeScale(key.Keycode switch
            {
                Key.F2 => SandboxTimeScaleMath.Adjust(_sandboxTimeScale, -1),
                Key.F3 => SandboxTimeScaleMath.DefaultScale,
                Key.F4 => SandboxTimeScaleMath.Adjust(_sandboxTimeScale, 1),
                _ => _sandboxTimeScale,
            });
            GetViewport().SetInputAsHandled();
            return;
        }

        var preset = key.Keycode switch
        {
            Key.F6 => SandboxAtmospherePreset.Daytime,
            Key.F7 => SandboxAtmospherePreset.Dusk,
            Key.F8 => SandboxAtmospherePreset.Night,
            Key.F9 => SandboxAtmospherePreset.SignalRestoration,
            Key.F10 => SandboxAtmospherePreset.Corruption,
            _ => (SandboxAtmospherePreset?)null,
        };

        if (preset is null)
        {
            return;
        }

        ApplySandboxAtmosphere(preset.Value);
        GetViewport().SetInputAsHandled();
    }

    private void OnSelectionChanged(int count)
    {
        RefreshSelectionInfo();
        if (count > 0)
        {
            PlayAudioCue(TacticalAudioCue.Selection);
        }
    }
}
