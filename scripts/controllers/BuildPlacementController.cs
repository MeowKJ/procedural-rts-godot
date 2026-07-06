using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Controllers;

public partial class BuildPlacementController : Node2D
{
    public required GameState State { get; init; }
    public required UnitBattlefield UnitBattlefield { get; init; }
    public required Camera2D Camera { get; init; }
    public PlayerSlotId LocalPlayerSlotId { get; init; } = PlayerSlotId.One;
    public UnitFactionId LocalFaction { get; init; } = UnitFactionId.Dog;
    public Action<string>? StatusChanged { get; init; }
    public Action<CommandAcknowledgementKind, Vector2, CommandAcknowledgementAudioCue>? CommandAcknowledged { get; init; }

    private static readonly string[] BuildOrder =
    [
        BuildingDesignIds.PowerPlant,
        BuildingDesignIds.Barracks,
        BuildingDesignIds.VehicleFactory,
        BuildingDesignIds.Refinery,
        BuildingDesignIds.Headquarters,
        BuildingDesignIds.GroundTurret,
        BuildingDesignIds.Airfield,
        BuildingDesignIds.AntiAirTurret,
    ];

    private int _selectedIndex = -1;
    private EntityId _activeReadyTicketId = EntityId.None;
    private float _previewRotation;

    public bool IsActive => _selectedIndex >= 0;
    private bool HasActiveReadyTicket => _activeReadyTicketId.IsValid;
    public CommandPreviewState PreviewState { get; private set; } = CommandPreviewState.None;

    public bool SelectBuildCategory(BuildCategory category)
    {
        for (var index = 0; index < BuildOrder.Length; index++)
        {
            var spec = BuildSpecCatalog.For(BuildOrder[index]);
            if (spec.Category != category)
            {
                continue;
            }

            _activeReadyTicketId = EntityId.None;
            _selectedIndex = index;
            var key = ShouldQueueConstructionTicket(spec.Kind) ? "build.queuePreview" : "build.preview";
            StatusChanged?.Invoke(GameText.Format(key, spec.Label));
            QueueRedraw();
            return true;
        }

        return false;
    }

    public override void _Process(double delta)
    {
        PreviewState = IsActive
            ? CreatePreviewState(GetViewport().GetMousePosition())
            : CommandPreviewState.None;

        if (IsActive)
        {
            QueueRedraw();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            HandleKey(key);
            return;
        }

        if (!IsActive || @event is not InputEventMouseButton mouse || !mouse.Pressed)
        {
            return;
        }

        if (mouse.ButtonIndex == MouseButton.Right)
        {
            CancelPreview();
            GetViewport().SetInputAsHandled();
        }
        else if (mouse.ButtonIndex == MouseButton.Left)
        {
            var kind = CurrentKind();
            var spec = CurrentSpec();
            var mouseWorld = ScreenToWorld(mouse.Position);
            var placement = UnitBattlefield.ValidateBuildingPlacement(kind, LocalPlayerSlotId, mouseWorld);
            bool accepted;
            string status;
            if (HasActiveReadyTicket)
            {
                accepted = UnitBattlefield.PlaceReadyConstructionTicket(
                    LocalPlayerSlotId,
                    LocalFaction,
                    _activeReadyTicketId,
                    mouseWorld,
                    out _,
                    out status,
                    _previewRotation);
            }
            else if (ShouldQueueConstructionTicket(kind))
            {
                accepted = QueueConstructionTicket(kind, spec, out status);
            }
            else
            {
                accepted = UnitBattlefield.ConstructBuilding(
                    LocalPlayerSlotId,
                    LocalFaction,
                    kind,
                    mouseWorld,
                    out _,
                    out status,
                    _previewRotation);
            }

            StatusChanged?.Invoke(accepted
                ? status
                : GameText.Format("build.cannotPlace", spec.Label, PlacementStatusLabel(status, placement.Reason, spec)));
            if (accepted)
            {
                ClearActivePreview();
            }

            if (!accepted)
            {
                CommandAcknowledged?.Invoke(
                    CommandAcknowledgementKind.Invalid,
                    new Vector2(placement.X, placement.Y),
                    CommandAcknowledgementAudioCue.Invalid);
            }
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Draw()
    {
        if (!IsActive)
        {
            PreviewState = CommandPreviewState.None;
            return;
        }

        var spec = CurrentSpec();
        var mouseWorld = ScreenToWorld(GetViewport().GetMousePosition());
        var placement = UnitBattlefield.ValidateBuildingPlacement(CurrentKind(), LocalPlayerSlotId, mouseWorld);
        var queuePreview = ShouldQueueConstructionTicket(CurrentKind()) && !HasActiveReadyTicket;
        var placementValid = HasEnoughCreditsForPreview(spec) && (queuePreview || placement.IsValid);
        var accent = placementValid ? spec.Accent : new Color("#ff5d75");
        var rect = new Rect2(-spec.Footprint / 2f, spec.Footprint);
        var pulse = 0.58f + Mathf.Sin(Time.GetTicksMsec() / 110f) * 0.22f;

        DrawSetTransform(new Vector2(placement.X, placement.Y), _previewRotation, Vector2.One);
        DrawFootprintPreview(rect, accent, pulse, placementValid);
        DrawStructurePreview(rect, accent, pulse, placementValid);
        DrawPlacementCursor(rect, accent, placementValid);
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    private void HandleKey(InputEventKey key)
    {
        switch (key.Keycode)
        {
            case Key.B:
                CyclePreview(key.ShiftPressed ? -1 : 1);
                GetViewport().SetInputAsHandled();
                break;
            case Key.R when IsActive:
                _previewRotation = NormalizePreviewRotation(_previewRotation + Mathf.Pi / 2f);
                StatusChanged?.Invoke(GameText.Format("build.rotated", CurrentSpec().Label));
                GetViewport().SetInputAsHandled();
                break;
            case Key.Y when IsActive:
                CancelPreview();
                GetViewport().SetInputAsHandled();
                break;
            case Key.Escape when IsActive:
                CancelPreview();
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    private void CyclePreview(int direction)
    {
        if (TryCycleReadyTicket(direction))
        {
            return;
        }

        _activeReadyTicketId = EntityId.None;
        _selectedIndex = _selectedIndex < 0
            ? 0
            : PosMod(_selectedIndex + direction, BuildOrder.Length);

        var spec = CurrentSpec();
        var key = ShouldQueueConstructionTicket(CurrentKind()) ? "build.queuePreview" : "build.preview";
        StatusChanged?.Invoke(GameText.Format(key, spec.Label));
    }

    private void CancelPreview()
    {
        var wasActive = IsActive;
        if (TryCancelActiveReadyTicket())
        {
            return;
        }

        ClearActivePreview();

        if (wasActive)
        {
            StatusChanged?.Invoke(GameText.T("build.cancelled"));
            QueueRedraw();
        }
    }

    private bool TryCancelActiveReadyTicket()
    {
        if (!HasActiveReadyTicket)
        {
            return false;
        }

        UnitBattlefield.CancelConstructionTicket(LocalPlayerSlotId, _activeReadyTicketId, out var status);
        StatusChanged?.Invoke(status);
        ClearActivePreview();
        QueueRedraw();
        return true;
    }

    private BuildSpec CurrentSpec() => BuildSpecCatalog.For(CurrentKind());

    private string CurrentKind() => BuildOrder[_selectedIndex];

    private bool TryCycleReadyTicket(int direction)
    {
        var tickets = UnitBattlefield.ReadyConstructionTickets(LocalPlayerSlotId);
        if (tickets.Count == 0)
        {
            _activeReadyTicketId = EntityId.None;
            return false;
        }

        var currentIndex = -1;
        for (var index = 0; index < tickets.Count; index++)
        {
            if (tickets[index].EntityId == _activeReadyTicketId)
            {
                currentIndex = index;
                break;
            }
        }

        var nextIndex = currentIndex < 0
            ? direction < 0 ? tickets.Count - 1 : 0
            : PosMod(currentIndex + direction, tickets.Count);
        var ticket = tickets[nextIndex];
        _activeReadyTicketId = ticket.EntityId;
        _selectedIndex = BuildIndexForKind(ticket.Kind);
        StatusChanged?.Invoke(GameText.Format("build.readyTicket", BuildSpecCatalog.For(ticket.Kind).Label));
        return true;
    }

    private bool QueueConstructionTicket(string kind, BuildSpec spec, out string status)
    {
        var ticket = UnitBattlefield.QueueConstructionTicket(LocalPlayerSlotId, kind, out var queueStatus);
        status = ticket is null ? queueStatus : GameText.Format("build.queued", spec.Label);
        return ticket is not null;
    }

    private bool ShouldQueueConstructionTicket(string kind)
    {
        return BuildSpecCatalog.For(kind)
            .ConstructionPolicy?
            .DefaultMethodFor(LocalFaction)
            .PlacementMode == BuildPlacementMode.SidebarPlacement;
    }

    private static int BuildIndexForKind(string kind)
    {
        for (var index = 0; index < BuildOrder.Length; index++)
        {
            if (BuildOrder[index] == kind)
            {
                return index;
            }
        }

        return 0;
    }

    private void ClearActivePreview()
    {
        _selectedIndex = -1;
        _activeReadyTicketId = EntityId.None;
        QueueRedraw();
    }

    private Vector2 ScreenToWorld(Vector2 screenPoint)
    {
        var viewportSize = GetViewportRect().Size;
        var zoom = Mathf.Max(Camera.Zoom.X, 0.001f);
        return Camera.GetScreenCenterPosition() + (screenPoint - viewportSize / 2f) / zoom;
    }

    private CommandPreviewState CreatePreviewState(Vector2 screenPoint)
    {
        var kind = CurrentKind();
        var spec = CurrentSpec();
        var mouseWorld = ScreenToWorld(screenPoint);
        var placement = UnitBattlefield.ValidateBuildingPlacement(kind, LocalPlayerSlotId, mouseWorld);
        var snapped = new Vector2(placement.X, placement.Y);
        var queuePreview = ShouldQueueConstructionTicket(kind) && !HasActiveReadyTicket;
        if (!HasEnoughCreditsForPreview(spec) && (queuePreview || placement.IsValid))
        {
            return new CommandPreviewState(
                CommandPreviewKind.BuildInvalid,
                PlacementStatusLabel("placement.needCredits", placement.Reason, spec).ToUpperInvariant(),
                screenPoint,
                snapped,
                false);
        }

        if (queuePreview)
        {
            return new CommandPreviewState(CommandPreviewKind.BuildValid, GameText.Format("build.queuePreview", spec.Label.ToUpperInvariant()), screenPoint, snapped, true);
        }

        var label = HasActiveReadyTicket
            ? GameText.Format("build.placeReadyPreview", spec.Label.ToUpperInvariant())
            : GameText.Format("build.placePreview", spec.Label.ToUpperInvariant());
        return placement.IsValid
            ? new CommandPreviewState(CommandPreviewKind.BuildValid, label, screenPoint, snapped, true)
            : new CommandPreviewState(CommandPreviewKind.BuildInvalid, PlacementReasonLabel(placement.Reason).ToUpperInvariant(), screenPoint, snapped, false);
    }

    private bool HasEnoughCreditsForPreview(BuildSpec spec)
    {
        return HasActiveReadyTicket || UnitBattlefield.Credits(LocalPlayerSlotId) >= spec.Cost;
    }

    private static string PlacementStatusLabel(string status, string fallbackReason, BuildSpec spec)
    {
        return status == "placement.needCredits"
            ? GameText.Format("ui.needCredits", spec.Cost)
            : PlacementReasonLabel(status.StartsWith("placement.", StringComparison.Ordinal) ? status : fallbackReason);
    }

    private static string PlacementReasonLabel(string reason)
    {
        return reason switch
        {
            "placement.outside" => GameText.T("placement.outside"),
            "placement.blocked" => GameText.T("placement.blocked"),
            "placement.outsideBuildRadius" => GameText.T("placement.outsideBuildRadius"),
            "placement.unpowered" => GameText.T("placement.unpowered"),
            "placement.impassable" => GameText.T("placement.impassable"),
            "placement.notVisible" => GameText.T("placement.notVisible"),
            "placement.ready" => GameText.T("placement.ready"),
            _ => reason,
        };
    }

    private static int PosMod(int value, int modulo)
    {
        return ((value % modulo) + modulo) % modulo;
    }

    private static float NormalizePreviewRotation(float rotation)
    {
        var normalized = rotation % Mathf.Tau;
        return normalized < 0 ? normalized + Mathf.Tau : normalized;
    }
}
