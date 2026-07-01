using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// Immutable-per-tick movement tuning for an entity on the EntityWorld path.
/// Separate from <see cref="MovementComponentState"/> (which holds mutable
/// velocity/target) so the existing movement record and its bridges/tests are
/// untouched while the sim core is stood up.
/// </summary>
public sealed record MovementProfileComponentState(
    float MaxSpeed,
    float ArriveRadius = 2f) : EntityComponentState;
