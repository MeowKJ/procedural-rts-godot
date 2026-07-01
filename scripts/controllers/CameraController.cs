using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Controllers;

public partial class CameraController : Camera2D
{
    [Export] public Vector2 WorldSize { get; set; } = new(3600, 2400);
    [Export] public bool InputEnabled { get; set; } = true;

    private const float MinZoom = 0.42f;
    private const float MaxZoom = 1.45f;
    private const float EdgeScrollPixels = 28;
    private const float PanResponsiveness = 18f;
    private const float ZoomResponsiveness = 20f;
    private const float ViewChangeNotifyInterval = 0.05f;
    private const float ViewChangePositionEpsilonSquared = 9f;
    private const float ViewChangeZoomEpsilon = 0.004f;
    private const float ViewChangeImmediatePositionEpsilonSquared = 900f;
    private const float ViewChangeImmediateZoomEpsilon = 0.035f;
    private Vector2 _targetPosition = new(900, 820);
    private float _targetZoom = 0.82f;
    private Vector2 _lastViewChangePosition;
    private float _lastViewChangeZoom;
    private float _viewChangeNotifyTimer;
    public event Action? ViewChanged;

    public override void _Ready()
    {
        Enabled = true;
        Position = _targetPosition;
        Zoom = Vector2.One * _targetZoom;
        _lastViewChangePosition = Position;
        _lastViewChangeZoom = Zoom.X;
    }

    public override void _Process(double delta)
    {
        var input = InputEnabled ? Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down") : Vector2.Zero;
        if (InputEnabled)
        {
            if (Input.IsKeyPressed(Key.W)) input.Y -= 1;
            if (Input.IsKeyPressed(Key.S)) input.Y += 1;
            if (Input.IsKeyPressed(Key.A)) input.X -= 1;
            if (Input.IsKeyPressed(Key.D)) input.X += 1;

            input += EdgeScrollInput();
        }

        if (input.LengthSquared() > 1)
        {
            input = input.Normalized();
        }

        var frameDt = (float)delta;
        var dt = CameraInputMath.StableVisualDelta(frameDt);
        _viewChangeNotifyTimer -= frameDt;
        var speed = 680 / Mathf.Max(_targetZoom, 0.001f);
        _targetPosition = ClampToWorld(_targetPosition + input * speed * dt);

        var smoothedPosition = CameraInputMath.SmoothToward(
            Position.X,
            Position.Y,
            _targetPosition.X,
            _targetPosition.Y,
            PanResponsiveness,
            dt);
        Position = ClampToWorld(new Vector2(smoothedPosition.X, smoothedPosition.Y));
        var zoom = CameraInputMath.SmoothToward(Zoom.X, _targetZoom, ZoomResponsiveness, dt);
        Zoom = Vector2.One * zoom;

        NotifyViewChangedIfNeeded();
    }

    public void FocusOnWorldPoint(Vector2 worldPoint)
    {
        _targetPosition = ClampToWorld(worldPoint);
    }

    public void SnapToWorldPoint(Vector2 worldPoint)
    {
        _targetPosition = ClampToWorld(worldPoint);
        Position = _targetPosition;
        _lastViewChangePosition = Position;
        _lastViewChangeZoom = Zoom.X;
        ViewChanged?.Invoke();
    }

    public Rect2 VisibleWorldRect()
    {
        var size = GetViewportRect().Size / Zoom;
        return new Rect2(Position - size / 2f, size);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true } mouse)
        {
            if (mouse.ButtonIndex == MouseButton.WheelUp)
            {
                SetZoom(Zoom.X * 1.08f);
            }
            else if (mouse.ButtonIndex == MouseButton.WheelDown)
            {
                SetZoom(Zoom.X / 1.08f);
            }
        }
    }

    private void SetZoom(float zoom)
    {
        var next = Mathf.Clamp(zoom, MinZoom, MaxZoom);
        if (Mathf.IsEqualApprox(Zoom.X, next))
        {
            return;
        }

        _targetZoom = next;
    }

    private void NotifyViewChangedIfNeeded()
    {
        var positionDeltaSquared = Position.DistanceSquaredTo(_lastViewChangePosition);
        var zoomDelta = Mathf.Abs(Zoom.X - _lastViewChangeZoom);
        if (positionDeltaSquared <= ViewChangePositionEpsilonSquared
            && zoomDelta <= ViewChangeZoomEpsilon)
        {
            return;
        }

        var shouldNotifyImmediately = positionDeltaSquared >= ViewChangeImmediatePositionEpsilonSquared
            || zoomDelta >= ViewChangeImmediateZoomEpsilon;
        if (_viewChangeNotifyTimer > 0 && !shouldNotifyImmediately)
        {
            return;
        }

        _lastViewChangePosition = Position;
        _lastViewChangeZoom = Zoom.X;
        _viewChangeNotifyTimer = ViewChangeNotifyInterval;
        ViewChanged?.Invoke();
    }

    private Vector2 EdgeScrollInput()
    {
        var viewport = GetViewport();
        if (!GetWindow().HasFocus())
        {
            return Vector2.Zero;
        }

        var mouse = viewport.GetMousePosition();
        var viewportSize = GetViewportRect().Size;
        var direction = CameraInputMath.EdgeScrollDirection(mouse.X, mouse.Y, viewportSize.X, viewportSize.Y, EdgeScrollPixels);
        return new Vector2(direction.X, direction.Y);
    }

    private Vector2 ClampToWorld(Vector2 position)
    {
        return new Vector2(Mathf.Clamp(position.X, 0, WorldSize.X), Mathf.Clamp(position.Y, 0, WorldSize.Y));
    }
}
