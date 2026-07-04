using Godot;

namespace ProceduralRts.Core;

public sealed record UnitSpecRuntimeDescriptor(
    string DesignId,
    UnitArchetype Archetype,
    UnitFactionId Faction,
    string Label,
    UnitWeightClass WeightClass,
    MovementDomain MovementDomain,
    ArmorTag ArmorTag,
    WeaponKind WeaponKind,
    float MaxHp,
    float Radius,
    float Speed,
    float TurnRate,
    TurnMode TurnMode,
    float SightRange,
    float AttackRange,
    float Damage,
    float AttackCooldown,
    float ProjectileSpeed,
    Color Accent,
    int TechTier,
    IReadOnlySet<UnitRoleTag> RoleTags,
    ElementDefenseProfile? ElementDefense,
    TargetTraitProfile? TargetTraits);
