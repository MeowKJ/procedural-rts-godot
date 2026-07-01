static partial class Program
{
    static EntitySpec CombatSpec()
    {
        return new EntitySpec
        {
            Id = "replay.soldier",
            Kind = EntityKind.Unit,
            Display = new EntityDisplaySpec("Soldier", "replay.soldier.name", "replay.soldier.role", "SLD", IconGlyph.Infantry),
            Stats = new StatsSpec(UnitWeightClass.Medium, ArmorTag.Vehicle, MaxHp: 120, SightRange: 700, Cost: 100, TechTier: 1),
            Movement = new MovementSpec(MovementDomain.Land, Speed: 120, TurnRate: 6),
            Collision = new CollisionSpec(Radius: 14, Mass: 1, PushPriority: 1),
            Weapons = new[]
            {
                WeaponMountSpec.Independent("main", WeaponKind.NeedleRifle, Vector2.Zero, new Vector2(14, 0), MathF.Tau, 8, fireWhileMoving: true),
            },
        };
    }

    static void SpawnSoldier(EntityWorld world, EntitySpec spec, OwnerId owner, Vector2 at)
    {
        world.Spawn(spec, owner, EntityTransform.At(at), new EntityComponentState[]
        {
            new HealthComponentState(spec.Stats!.MaxHp, spec.Stats!.MaxHp),
            new MovementComponentState(Velocity: default),
            new MovementProfileComponentState(MaxSpeed: spec.Movement!.Speed),
            new VisionComponentState(spec.Stats!.SightRange),
            new StanceComponentState(UnitStance.Aggressive),
            new WeaponUserComponentState(new[]
            {
                new WeaponMountRuntimeState("main", WeaponKind.NeedleRifle, 0, 0),
            }),
        });
    }
}
