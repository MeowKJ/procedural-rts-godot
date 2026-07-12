using Godot;

namespace ProceduralRts.Core;

public readonly record struct CommandPreviewState(
    CommandPreviewKind Kind,
    string Label,
    Vector2 ScreenPosition,
    Vector2 WorldPosition,
    bool IsValid,
    CommandPreviewPhase Phase = CommandPreviewPhase.PassiveHover)
{
    public static CommandPreviewState None { get; } = new(
        CommandPreviewKind.None,
        "",
        Vector2.Zero,
        Vector2.Zero,
        false,
        CommandPreviewPhase.None);
}
