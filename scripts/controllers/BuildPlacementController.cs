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
    ];

    private int _selectedIndex = -1;
    private EntityId _activeReadyTicketId = EntityId.None;
    private float _previewRotation;

    public bool IsActive => _selectedIndex >= 0;
    private bool HasActiveReadyTicket => _activeReadyTicketId.IsValid;
    public CommandPreviewState PreviewState { get; private set; } = CommandPreviewState.None;

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
                _previewRotation += Mathf.Pi / 2f;
                StatusChanged?.Invoke(GameText.Format("build.rotated", CurrentSpec().Label));
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
        ClearActivePreview();

        if (wasActive)
        {
            StatusChanged?.Invoke(GameText.T("build.cancelled"));
            QueueRedraw();
        }
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

    private void DrawFootprintPreview(Rect2 rect, Color accent, float pulse, bool isValid)
    {
        var pad = 14;
        var footprint = rect.Grow(pad);
        DrawRect(footprint, new Color(accent, isValid ? 0.11f : 0.16f), true);
        DrawRect(footprint, new Color("#ffffff", isValid ? 0.22f + pulse * 0.18f : 0.08f), false, 1.4f);
        DrawRect(footprint, new Color(accent, isValid ? 0.62f : 0.86f), false, 3.4f);

        var step = 32f;
        for (var x = footprint.Position.X + step; x < footprint.End.X; x += step)
        {
            DrawLine(new Vector2(x, footprint.Position.Y), new Vector2(x, footprint.End.Y), new Color(accent, 0.16f), 1, true);
        }

        for (var y = footprint.Position.Y + step; y < footprint.End.Y; y += step)
        {
            DrawLine(new Vector2(footprint.Position.X, y), new Vector2(footprint.End.X, y), new Color(accent, 0.16f), 1, true);
        }
    }

    private void DrawStructurePreview(Rect2 rect, Color accent, float pulse, bool isValid)
    {
        DrawRect(rect, new Color("#07111d", isValid ? 0.44f : 0.3f), true);
        DrawRect(rect, new Color(accent, 0.82f), false, 2.2f);

        var centerGlow = Mathf.Min(rect.Size.X, rect.Size.Y) * (0.2f + pulse * 0.025f);
        DrawCircle(Vector2.Zero, centerGlow, new Color(accent, 0.16f));

        switch (BuildOrder[_selectedIndex])
        {
            case BuildingDesignIds.PowerPlant:
                DrawArc(Vector2.Zero, Mathf.Min(rect.Size.X, rect.Size.Y) * 0.28f, 0, Mathf.Tau, 72, new Color("#ffffff", 0.62f), 2.2f, true);
                break;
            case BuildingDesignIds.Barracks:
                DrawLine(new Vector2(rect.Position.X + 18, 0), new Vector2(rect.End.X - 18, 0), new Color("#ffffff", 0.5f), 2, true);
                break;
            case BuildingDesignIds.VehicleFactory:
                DrawRect(new Rect2(rect.Position + new Vector2(22, rect.Size.Y * 0.5f), new Vector2(rect.Size.X - 44, rect.Size.Y * 0.28f)), new Color("#ffffff", 0.18f), true);
                break;
            case BuildingDesignIds.Refinery:
                DrawCircle(new Vector2(-rect.Size.X * 0.18f, 0), rect.Size.Y * 0.16f, new Color("#ffffff", 0.18f));
                DrawCircle(new Vector2(rect.Size.X * 0.18f, 0), rect.Size.Y * 0.16f, new Color("#ffffff", 0.18f));
                break;
            case BuildingDesignIds.Headquarters:
                DrawArc(Vector2.Zero, Mathf.Min(rect.Size.X, rect.Size.Y) * 0.24f, 0, Mathf.Tau, 72, new Color(accent, 0.9f), 2.6f, true);
                DrawLine(new Vector2(0, rect.Position.Y + 12), new Vector2(0, rect.End.Y - 12), new Color("#ffffff", 0.45f), 1.8f, true);
                break;
        }
    }

    private void DrawPlacementCursor(Rect2 rect, Color accent, bool isValid)
    {
        var size = rect.Size;
        var half = size / 2f;
        var color = isValid ? new Color("#ffffff", 0.72f) : new Color("#ff5d75", 0.88f);
        const float arm = 26;

        DrawLine(new Vector2(-half.X - arm, 0), new Vector2(-half.X - 6, 0), color, 2.4f, true);
        DrawLine(new Vector2(half.X + 6, 0), new Vector2(half.X + arm, 0), color, 2.4f, true);
        DrawLine(new Vector2(0, -half.Y - arm), new Vector2(0, -half.Y - 6), color, 2.4f, true);
        DrawLine(new Vector2(0, half.Y + 6), new Vector2(0, half.Y + arm), color, 2.4f, true);

        if (isValid)
        {
            DrawCircle(Vector2.Zero, 6, new Color(accent, 0.68f));
            return;
        }

        DrawLine(new Vector2(-18, -18), new Vector2(18, 18), color, 3.2f, true);
        DrawLine(new Vector2(-18, 18), new Vector2(18, -18), color, 3.2f, true);
    }

    private static int PosMod(int value, int modulo)
    {
        return ((value % modulo) + modulo) % modulo;
    }
}
