using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// Engagement stance for an armed entity on the EntityWorld path. Governs how
/// auto-acquire and shared-threat respond (see UnitStance). Separate component
/// so non-combat entities simply omit it.
/// </summary>
public sealed record StanceComponentState(
    UnitStance Stance = UnitStance.Aggressive,
    Vector2? AnchorPosition = null) : EntityComponentState;
