using Godot;
using ProceduralRts.Controllers;
using ProceduralRts.Core;
using ProceduralRts.Ui;
using ProceduralRts.World;

namespace ProceduralRts;

public partial class BattleRoot
{
    private void ApplySandboxLaunchState()
    {
        if (_state.Options.LaunchMode != LaunchMode.Sandbox)
        {
            return;
        }

        _state.ClearSelection();
        CollectSandboxLaunchSelectionIds();
        _unitBattlefield.SelectUnitsByIds(PlayerSlotId.One, _sandboxLaunchUnitIdBuffer);
        _camera.FocusOnWorldPoint(SandboxLaunchFocus);
        RefreshSelectionInfo();
        RefreshMinimap();
        ApplySandboxRelationsFromContext();
        _hud.SetSandboxDeveloperControlsVisible(true);
        _hud.SetSandboxDeveloperContext(_sandboxContext);
        RefreshSandboxStateHash();
        RefreshSandboxCommandLog();
        _hud.SetStatus("Developer sandbox ready: context panel, F2-F4 time, F6-F10 atmosphere");
        AddAlert(AlertKind.Production, "Sandbox loaded: context panel plus stress spawn", new Vector2(980, 940));
    }

    private void OnSandboxDeveloperContextRequested(SandboxDeveloperContextRequest request)
    {
        ApplySandboxContextRequest(request);
    }

    private void OnSandboxStressRequested()
    {
        if (_state.Options.LaunchMode != LaunchMode.Sandbox)
        {
            return;
        }

        var plan = SandboxStressSpawnPlanner.Create(
            _sandboxContext,
            SandboxStressCenter(),
            SandboxFacingForContext());
        var spawned = 0;
        foreach (var request in plan.Requests)
        {
            if (request.Spec.Kind == EntityKind.Unit)
            {
                SpawnSandboxUnit(request);
                spawned++;
            }
            else if (TrySpawnSandboxStructure(request))
            {
                spawned++;
            }
        }

        _sandboxStressRunIndex++;
        var status = spawned > 0
            ? $"{plan.FormatStatus()} (run {_sandboxStressRunIndex})"
            : plan.FormatStatus();
        _hud.SetStatus(status);
        AddAlert(AlertKind.Production, status, SandboxAlertPosition());
        RefreshMinimap();
        RefreshCommandCard();
        RefreshSelectionInfo();
        RefreshViewCulling();
    }

    private void ApplySandboxContextRequest(SandboxDeveloperContextRequest request, string? statusOverride = null)
    {
        _sandboxContext = _sandboxContext.Apply(request);
        _sandboxTimeScale = _sandboxContext.TimeScale;
        ApplySandboxRelationsFromContext();
        _state.ApplySandboxAtmosphere(_sandboxContext.Environment);
        _hud.SetSandboxDeveloperContext(_sandboxContext);
        RefreshSandboxStateHash();
        RefreshSandboxCommandLog();
        _hud.SetStatus(statusOverride ?? _sandboxContext.FormatStatus());
        RefreshCommandPreview();
        RefreshMinimap();
    }

    private void RefreshSandboxStateHash()
    {
        if (_state.Options.LaunchMode != LaunchMode.Sandbox
            || !_sandboxContext.DebugOverlay.IsEnabled(SandboxDebugOverlayFlag.StateHash))
        {
            _hud.SetSandboxStateHash(null);
            return;
        }

        _hud.SetSandboxStateHash(_unitBattlefield.EntityWorld.DeterministicStateHash());
    }

    private void RefreshSandboxCommandLog()
    {
        var visible = _state.Options.LaunchMode == LaunchMode.Sandbox
            && _sandboxContext.DebugOverlay.IsEnabled(SandboxDebugOverlayFlag.CommandLog);
        _hud.SetSandboxCommandLog(_sandboxCommandLogLines, visible);
    }

    private void ApplySandboxRelationsFromContext()
    {
        var playerSlot = _sandboxContext.OwnerId.ToPlayerSlot();
        foreach (var subject in new[] { PlayerSlotId.One, PlayerSlotId.Two, PlayerSlotId.Three, PlayerSlotId.Four })
        {
            if (subject == playerSlot)
            {
                continue;
            }

            _unitBattlefield.Relations.Set(playerSlot, subject, _sandboxContext.Relation);
            _entityWorld.Relations.Set(_sandboxContext.OwnerId, OwnerId.FromPlayerSlot(subject), _sandboxContext.Relation);
        }
    }

    private void SpawnSandboxUnit(SandboxSpawnRequest request)
    {
        var unit = _unitBattlefield.Spawn(
            request.Entry.Id,
            request.OwnerId.ToPlayerSlot(),
            ClampSandboxWorldPoint(request.Transform.Position, 32),
            request.Transform.Facing);
        SetUnitInstanceFacing(unit, request.Transform.Facing);
        AddUnitInstanceView(unit);
    }

    private bool TrySpawnSandboxStructure(SandboxSpawnRequest request)
    {
        if (request.Spec.Authoring.BuildingSpecId is not { } kind)
        {
            return false;
        }

        var spec = BuildSpecCatalog.For(kind);
        var playerSlot = request.OwnerId.ToPlayerSlot();
        var position = ClampSandboxWorldPoint(
            request.Transform.Position,
            MathF.Max(spec.Footprint.X, spec.Footprint.Y) * 0.5f + 8);
        var nextId = NextSandboxBuildingTargetId();
        var building = _unitBattlefield.UpsertBuildingTarget(
            nextId,
            kind,
            playerSlot,
            _sandboxContext.Faction,
            position,
            request.Transform.Facing,
            spec.MaxHp);
        var viewModel = new BuildingModel
        {
            Id = building.Id,
            Kind = kind,
            Owner = LegacyOwnerForPlayerSlot(playerSlot) ?? ProceduralRts.Core.Owner.Enemy,
            FactionId = ToLegacyFaction(_sandboxContext.Faction),
            Position = position,
            Facing = request.Transform.Facing,
            TurretFacing = request.Transform.Facing,
            Hp = spec.MaxHp,
        };
        var view = CreateBuildingView(viewModel);
        AddChild(view);
        _buildingViews[building.Id] = view;
        return true;
    }

    private void CollectSandboxLaunchSelectionIds()
    {
        _sandboxLaunchUnitBuffer.Clear();
        _sandboxLaunchUnitIdBuffer.Clear();
        foreach (var unit in _unitBattlefield.Units)
        {
            if (unit.PlayerSlotId == PlayerSlotId.One
                && !unit.Spec.RoleTags.Contains(UnitRoleTag.Economy))
            {
                _sandboxLaunchUnitBuffer.Add(unit);
            }
        }

        _sandboxLaunchUnitBuffer.Sort(CompareSandboxLaunchUnits);
        var selectedCount = Math.Min(6, _sandboxLaunchUnitBuffer.Count);
        for (var index = 0; index < selectedCount; index++)
        {
            _sandboxLaunchUnitIdBuffer.Add(_sandboxLaunchUnitBuffer[index].Id);
        }
    }

    private int NextSandboxBuildingTargetId()
    {
        var nextId = 1;
        foreach (var building in _unitBattlefield.BuildingSnapshots())
        {
            if (building.Id >= nextId)
            {
                nextId = building.Id + 1;
            }
        }

        return nextId;
    }

    private static int CompareSandboxLaunchUnits(UnitInstance left, UnitInstance right)
    {
        var distance = left.Position.DistanceSquaredTo(SandboxLaunchFocus)
            .CompareTo(right.Position.DistanceSquaredTo(SandboxLaunchFocus));
        return distance != 0 ? distance : left.Id.CompareTo(right.Id);
    }

    private Vector2 SandboxStressCenter()
    {
        var basePoint = _camera.GetScreenCenterPosition();
        var ring = new Vector2(140 + _sandboxStressRunIndex * 18, 0).Rotated(_sandboxStressRunIndex * 0.72f);
        return ClampSandboxWorldPoint(basePoint + ring, 220);
    }

    private float SandboxFacingForContext()
    {
        return _sandboxContext.OwnerId.Value % 2 == 0 ? Mathf.Pi : 0;
    }

    private Vector2 ClampSandboxWorldPoint(Vector2 point, float margin)
    {
        return new Vector2(
            Mathf.Clamp(point.X, margin, _state.WorldSize.X - margin),
            Mathf.Clamp(point.Y, margin, _state.WorldSize.Y - margin));
    }

    private static Vector2 SandboxAlertPosition()
    {
        return new Vector2(980, 940);
    }

    private static ProceduralRts.Core.Owner? LegacyOwnerForPlayerSlot(PlayerSlotId playerSlotId)
    {
        if (playerSlotId == PlayerSlotId.One)
        {
            return ProceduralRts.Core.Owner.Player;
        }

        if (playerSlotId == PlayerSlotId.Two)
        {
            return ProceduralRts.Core.Owner.Enemy;
        }

        return null;
    }

    private void ApplySandboxAtmosphere(SandboxAtmospherePreset preset)
    {
        ApplySandboxContextRequest(
            new SandboxDeveloperContextRequest(Environment: preset),
            SandboxAtmosphereStatus(preset));
    }

    private void ApplySandboxTimeScale(float scale)
    {
        ApplySandboxContextRequest(
            new SandboxDeveloperContextRequest(TimeScale: scale),
            SandboxTimeScaleMath.Format(scale));
    }

    private static string SandboxAtmosphereStatus(SandboxAtmospherePreset preset)
    {
        return $"Sandbox atmosphere: {SandboxDeveloperContextOptions.EnvironmentOption(preset).Label}";
    }
}
