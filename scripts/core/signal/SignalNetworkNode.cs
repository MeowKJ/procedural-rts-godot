using Godot;

namespace ProceduralRts.Core;

public sealed record SignalNetworkNode(
    int Id,
    SignalNodeKind Kind,
    Vector2 Position,
    float DayControlRadius,
    float NightVisionRadius,
    bool Powered);
