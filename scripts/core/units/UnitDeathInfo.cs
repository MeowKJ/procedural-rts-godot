using Godot;

namespace ProceduralRts.Core;

public readonly record struct UnitDeathInfo(
    int Id,
    string DesignId,
    Owner Owner,
    FactionId FactionId,
    Vector2 Position,
    float Radius,
    UnitWeightClass WeightClass,
    MovementDomain MovementDomain,
    string? KillingAmmoId,
    float OverkillDamage);
