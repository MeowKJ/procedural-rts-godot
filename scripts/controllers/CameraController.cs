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
    private const float MinImpactShakePixels = 0.18f;
    private const float MaxImpactShakePixels = 7f;
    private Vector2 _targetPosition = new(900, 820);
    private float _targetZoom = 0.82f;
    private Vector2 _lastViewChangePosition;
    private float _lastViewChangeZoom;
    private float _viewChangeNotifyTimer;
    private float _impactShakeRemaining;
    private float _impactShakeDuration;
    private float _impactShakeAmplitude;
    private float _impactShakePhase;
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

        UpdateImpactShake(frameDt);
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

    public void RequestImpactShake(Vector2 worldPoint, ImpactVfxStyle style)
    {
        if (style.ShakeAmplitude <= 0 || style.ShakeRadius <= 0)
        {
            return;
        }

        var falloff = ShakeFalloff(worldPoint, VisibleWorldRect(), style.ShakeRadius);
        var amplitude = Mathf.Clamp(style.ShakeAmplitude * falloff, 0, MaxImpactShakePixels);
        if (amplitude < MinImpactShakePixels)
        {
            return;
        }

        var duration = Mathf.Clamp(0.13f + amplitude * 0.018f, 0.13f, 0.24f);
        _impactShakeAmplitude = Mathf.Max(_impactShakeAmplitude, amplitude);
        _impactShakeDuration = Mathf.Max(_impactShakeDuration, duration);
        _impactShakeRemaining = Mathf.Max(_impactShakeRemaining, duration);
        _impactShakePhase += 1.618f;
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

    private void UpdateImpactShake(float delta)
    {
        if (_impactShakeRemaining <= 0)
        {
            Offset = Vector2.Zero;
            return;
        }

        _impactShakeRemaining = Mathf.Max(0, _impactShakeRemaining - delta);
        var progress = 1 - (_impactShakeRemaining / Mathf.Max(_impactShakeDuration, 0.001f));
        var fade = Mathf.Pow(1 - progress, 2);
        var angle = _impactShakePhase + progress * Mathf.Tau * 5.4f;
        Offset = new Vector2(Mathf.Sin(angle * 1.37f), Mathf.Cos(angle * 1.91f)) * _impactShakeAmplitude * fade;
        if (_impactShakeRemaining > 0)
        {
            return;
        }

        Offset = Vector2.Zero;
        _impactShakeAmplitude = 0;
        _impactShakeDuration = 0;
    }

    private static float ShakeFalloff(Vector2 point, Rect2 rect, float radius)
    {
        if (radius <= 0)
        {
            return 0;
        }

        if (rect.HasPoint(point))
        {
            return 1;
        }

        var end = rect.End;
        var dx = point.X < rect.Position.X ? rect.Position.X - point.X : point.X > end.X ? point.X - end.X : 0;
        var dy = point.Y < rect.Position.Y ? rect.Position.Y - point.Y : point.Y > end.Y ? point.Y - end.Y : 0;
        var distance = Mathf.Sqrt(dx * dx + dy * dy);
        return Mathf.Clamp(1 - distance / radius, 0, 1);
    }
}
