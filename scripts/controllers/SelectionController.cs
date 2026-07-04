using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts.Controllers;

public partial class SelectionController : Node2D
{
    private const float ActiveRedrawIntervalSeconds = 1f / 60f;
    private const float IdleRedrawIntervalSeconds = 1f / 30f;

    public required GameState State { get; init; }
    public required Camera2D Camera { get; init; }
    public UnitBattlefield? UnitBattlefield { get; init; }
    public PlayerSlotId LocalPlayerSlotId { get; init; } = PlayerSlotId.One;
    public Action<int>? SelectionChanged { get; init; }
    public Action<string>? StatusChanged { get; init; }
    public Action<TacticalAudioCue>? AudioCueRequested { get; init; }
    public Action<CommandAcknowledgementKind, Vector2, CommandAcknowledgementAudioCue>? CommandAcknowledged { get; init; }
    public Action<MoveCommandMode>? MoveModeRequested { get; init; }
    public Action<UnitStance>? UnitStanceRequested { get; init; }
    public Func<bool>? MouseInputBlocked { get; init; }
    public MoveCommandMode CurrentMoveMode { get; private set; } = MoveCommandMode.Direct;

    private Vector2? _dragStartScreen;
    private Vector2? _dragCurrentScreen;
    private Vector2? _dragStartWorld;
    private MouseButton? _dragButton;
    private double _dragStartSeconds;
    private bool _dragStartedAsDoubleClick;
    private UnitInstance? _hoveredUnitInstance;
    private UnitModel? _hoveredUnit;
    private BuildingHoverProjection? _hoveredBuildingProjection;
    private BuildingModel? _hoveredBuilding;
    private ResourceFieldModel? _hoveredResourceField;
    private readonly List<UnitModel> _legacySelectedUnitCommandBuffer = [];
    private readonly List<UnitModel> _legacyCommandLineUnitBuffer = [];
    private readonly List<BuildingModel> _legacyCommandLineBuildingBuffer = [];
    private readonly List<UnitInstance> _runtimeCommandLineUnitBuffer = [];
    private readonly List<int> _selectionHotkeyUnitIdBuffer = [];
    private readonly Dictionary<(int X, int Y), (Vector2 Position, Color Accent, float Pulse)> _commandLineTargetMarkers = [];
    private float _redrawTimer;

    public CommandPreviewState PreviewState { get; private set; } = CommandPreviewState.None;

    public override void _Process(double delta)
    {
        var mousePosition = GetViewport().GetMousePosition();
        var worldPosition = ScreenToWorld(mousePosition);
        _hoveredUnitInstance = UseUnitBattlefieldInput()
            ? UnitBattlefield!.PickAnyUnit(worldPosition, PickPaddingWorld())
            : null;
        _hoveredUnit = _hoveredUnitInstance is null
            ? State.PickAnyUnit(worldPosition, PickPaddingWorld())
            : null;
        _hoveredBuildingProjection = UseUnitBattlefieldInput() && _hoveredUnitInstance is null
            ? UnitBattlefield!.PickAnyBuildingHoverProjection(worldPosition, LocalPlayerSlotId, PickPaddingWorld())
            : null;
        _hoveredBuilding = _hoveredUnit is null && _hoveredBuildingProjection is null
            ? State.PickAnyBuilding(worldPosition, PickPaddingWorld())
            : null;
        _hoveredResourceField = _hoveredUnit is null && _hoveredBuildingProjection is null && _hoveredBuilding is null
            ? PickResourceField(worldPosition)
            : null;
        PreviewState = CreatePreviewState(mousePosition, worldPosition);
        _redrawTimer -= (float)delta;
        if (_redrawTimer <= 0)
        {
            _redrawTimer = _dragStartScreen is null
                ? IdleRedrawIntervalSeconds
                : ActiveRedrawIntervalSeconds;
            QueueRedraw();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouse)
        {
            if (MouseInputBlocked?.Invoke() == true)
            {
                ClearDrag();
                return;
            }

            if (mouse.ButtonIndex == MouseButton.Left)
            {
                if (mouse.Pressed)
                {
                    StartDrag(mouse.Position, mouse.ButtonIndex);
                    _dragStartedAsDoubleClick = mouse.DoubleClick;
                }
                else if (_dragStartScreen is not null && _dragStartWorld is not null)
                {
                    FinishSelection(mouse.Position, mouse.ShiftPressed, _dragStartedAsDoubleClick);
                }
            }
            else if (mouse.ButtonIndex == MouseButton.Right)
            {
                if (mouse.Pressed)
                {
                    StartDrag(mouse.Position, mouse.ButtonIndex);
                }
                else if (_dragButton == MouseButton.Right && _dragStartScreen is not null && _dragStartWorld is not null)
                {
                    var distance = _dragStartScreen.Value.DistanceTo(mouse.Position);
                    var elapsed = Time.GetTicksMsec() / 1000.0 - _dragStartSeconds;
                    if (SelectionGestureMath.IsRightSelectionDrag(distance, elapsed))
                    {
                        FinishSelection(mouse.Position, mouse.ShiftPressed, doubleClick: false);
                    }
                    else
                    {
                        FinishRightClickCommand(mouse.Position, MoveModeFromModifiers(mouse));
                    }
                }
            }
        }
        else if (@event is InputEventMouseMotion motion && _dragStartScreen is not null)
        {
            _dragCurrentScreen = motion.Position;
            QueueRedraw();
        }
        else if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (HandleSelectionHotkey(key) || HandleMoveModeHotkey(key) || HandleStanceHotkey(key))
            {
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public override void _Draw()
    {
        DrawCommandLines();
        DrawHoverAffordance();

        if (_dragStartScreen is null || _dragCurrentScreen is null)
        {
            return;
        }

        var rect = RectFromPoints(_dragStartScreen.Value, _dragCurrentScreen.Value);
        if (rect.Size.Length() < 4)
        {
            return;
        }

        var localRect = RectFromPoints(ScreenToWorld(rect.Position), ScreenToWorld(rect.End));
        DrawSelectionBox(localRect);
    }

    private void StartDrag(Vector2 screenPoint, MouseButton button)
    {
        _dragStartScreen = screenPoint;
        _dragCurrentScreen = screenPoint;
        _dragStartWorld = ScreenToWorld(screenPoint);
        _dragButton = button;
        _dragStartSeconds = Time.GetTicksMsec() / 1000.0;
        QueueRedraw();
    }

    private void ClearDrag()
    {
        _dragStartScreen = null;
        _dragCurrentScreen = null;
        _dragStartWorld = null;
        _dragButton = null;
        _dragStartSeconds = 0;
        _dragStartedAsDoubleClick = false;
        QueueRedraw();
    }
}
