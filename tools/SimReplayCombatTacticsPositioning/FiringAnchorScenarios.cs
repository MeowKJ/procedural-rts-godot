static partial class Program
{
    static void RunFiringAnchorScenario()
    {
        // ---- Scenario 6: firing anchor ------------------------------------------------
        // A unit that has just fired gets a short non-displaceable window. A rear mover
        // overlapping it must be pushed around while the shooter stays stable.
        const int FiringAnchorTicks = 80;

        EntitySpec AnchorSpec(string id, IconGlyph glyph)
        {
            return new EntitySpec
            {
                Id = id,
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec(id, $"{id}.name", $"{id}.role", id.ToUpperInvariant()[..3], glyph),
                Stats = new StatsSpec(UnitWeightClass.Medium, ArmorTag.Vehicle, MaxHp: 140, SightRange: 500, Cost: 100, TechTier: 1),
                Movement = new MovementSpec(MovementDomain.Land, Speed: 120, TurnRate: 6),
                Collision = new CollisionSpec(Radius: 18, Mass: 1, PushPriority: 1),
                Weapons =
                [
                    WeaponMountSpec.Independent("main", WeaponKind.NeedleRifle, Vector2.Zero, new Vector2(14, 0), MathF.Tau, 12, fireWhileMoving: true),
                ],
            };
        }

        EntityWorld BuildFiringAnchor()
        {
            var world = new EntityWorld(seed: 808);
            world.AddSystem(new CombatSystem());
            world.AddSystem(new ProjectileSystem());
            world.AddSystem(new MovementSystem());
            world.AddSystem(new SeparationSystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

            var shooterSpec = AnchorSpec("replay.anchor_shooter", IconGlyph.Tank);
            var moverSpec = AnchorSpec("replay.anchor_mover", IconGlyph.Tank);
            var targetSpec = AnchorSpec("replay.anchor_target", IconGlyph.Building);

            world.Spawn(shooterSpec, new OwnerId(1), EntityTransform.At(new Vector2(500, 500)), new EntityComponentState[]
            {
                new HealthComponentState(140, 140),
                new MovementComponentState(Vector2.Zero, new Vector2(760, 500)),
                new MovementProfileComponentState(MaxSpeed: 120, ArriveRadius: 4),
                new CollisionComponentState(Radius: 18, Mass: 1, PushPriority: 1, BlocksMovement: true),
                new VisionComponentState(500),
                new StanceComponentState(UnitStance.Hold),
                new WeaponUserComponentState(new[]
                {
                    new WeaponMountRuntimeState("main", WeaponKind.NeedleRifle, 0, 0),
                }, new EntityId(3), CombatTargetKind.Unit, AttackTargetIsManual: true),
            });
            world.Spawn(moverSpec, new OwnerId(1), EntityTransform.At(new Vector2(478, 500)), new EntityComponentState[]
            {
                new HealthComponentState(140, 140),
                new MovementComponentState(Vector2.Zero, new Vector2(560, 500)),
                new MovementProfileComponentState(MaxSpeed: 160, ArriveRadius: 4),
                new CollisionComponentState(Radius: 18, Mass: 1, PushPriority: 1, BlocksMovement: true),
            });
            world.Spawn(targetSpec, new OwnerId(2), EntityTransform.At(new Vector2(630, 500)), new EntityComponentState[]
            {
                new HealthComponentState(1000, 1000),
                new CollisionComponentState(Radius: 18, Mass: 1, PushPriority: 1, BlocksMovement: true),
            });

            return world;
        }

        AssertDeterministic("firing-anchor", BuildFiringAnchor, FiringAnchorTicks, 10);

        var anchorWorld = BuildFiringAnchor();
        var anchorClock = new SimClock();
        var shooterStart = anchorWorld.OrderedEntities.Single(entity => entity.Id.Value == 1).Transform.Position;
        anchorWorld.Step(1, anchorClock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
        var shooter = anchorWorld.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var mover = anchorWorld.OrderedEntities.Single(entity => entity.Id.Value == 2);
        var shooterMovement = shooter.Components.Require<MovementComponentState>();
        Assert(shooterMovement.FireAnchorRemaining > 0, "firing unit should receive a short fire-anchor window");
        Assert(shooterMovement.MoveTarget is null, "firing unit should hold position after firing in range");
        Assert(shooter.Transform.Position.DistanceTo(shooterStart) <= 0.01f, "firing anchor should not be displaced by a rear mover");

        for (var tick = 2; tick <= 6; tick++)
        {
            anchorWorld.Step(tick, anchorClock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
            anchorWorld.Events.Drain();
        }

        shooter = anchorWorld.OrderedEntities.Single(entity => entity.Id.Value == 1);
        mover = anchorWorld.OrderedEntities.Single(entity => entity.Id.Value == 2);
        var moverDistance = mover.Transform.Position.DistanceTo(shooterStart);
        Assert(shooter.Transform.Position.DistanceTo(shooterStart) <= 0.01f, "firing anchor should stay stable while rear mover separates");
        Assert(moverDistance > 35f, $"rear mover should be separated around the firing anchor, got distance {moverDistance:0.00}");
        Assert(anchorWorld.Metrics.AnchorPushEvents > 0, "command-feel metrics should count anchor-push events when movers yield to firing anchors");
        Console.WriteLine($"OK [firing-anchor metric]: shooter held at {shooter.Transform.Position}, mover yielded to {mover.Transform.Position}.");

        // A unit already holding a valid attack target is also an anchor between shots.
        // This catches the feel bug where incoming units shove a cooldown-gated shooter
        // out of its firing position before the next shot refreshes FireAnchorRemaining.
        EntityWorld BuildAttackingAnchor()
        {
            var world = new EntityWorld(seed: 809);
            world.AddSystem(new CombatSystem());
            world.AddSystem(new ProjectileSystem());
            world.AddSystem(new MovementSystem());
            world.AddSystem(new SeparationSystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

            var shooterSpec = AnchorSpec("replay.attacking_anchor_shooter", IconGlyph.Tank);
            var moverSpec = AnchorSpec("replay.attacking_anchor_mover", IconGlyph.Tank);
            var targetSpec = AnchorSpec("replay.attacking_anchor_target", IconGlyph.Building);

            world.Spawn(shooterSpec, new OwnerId(1), EntityTransform.At(new Vector2(500, 620)), new EntityComponentState[]
            {
                new HealthComponentState(140, 140),
                new MovementComponentState(Vector2.Zero),
                new MovementProfileComponentState(MaxSpeed: 120, ArriveRadius: 4),
                new CollisionComponentState(Radius: 18, Mass: 1, PushPriority: 1, BlocksMovement: true),
                new VisionComponentState(500),
                new StanceComponentState(UnitStance.Hold),
                new WeaponUserComponentState(new[]
                {
                    new WeaponMountRuntimeState("main", WeaponKind.NeedleRifle, 0, 5f),
                }, new EntityId(3), CombatTargetKind.Unit, AttackTargetIsManual: true),
            });
            world.Spawn(moverSpec, new OwnerId(1), EntityTransform.At(new Vector2(478, 620)), new EntityComponentState[]
            {
                new HealthComponentState(140, 140),
                new MovementComponentState(Vector2.Zero, new Vector2(560, 620)),
                new MovementProfileComponentState(MaxSpeed: 160, ArriveRadius: 4),
                new CollisionComponentState(Radius: 18, Mass: 1, PushPriority: 1, BlocksMovement: true),
            });
            world.Spawn(targetSpec, new OwnerId(2), EntityTransform.At(new Vector2(600, 620)), new EntityComponentState[]
            {
                new HealthComponentState(1000, 1000),
                new CollisionComponentState(Radius: 18, Mass: 1, PushPriority: 1, BlocksMovement: true),
            });

            return world;
        }

        AssertDeterministic("attacking-anchor", BuildAttackingAnchor, FiringAnchorTicks, 10);

        var attackingAnchorWorld = BuildAttackingAnchor();
        var attackingAnchorClock = new SimClock();
        var attackingShooterStart = attackingAnchorWorld.OrderedEntities.Single(entity => entity.Id.Value == 1).Transform.Position;
        attackingAnchorWorld.Step(1, attackingAnchorClock.FixedDelta, Array.Empty<SequencedCommandEnvelope>());
        var attackingShooter = attackingAnchorWorld.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var attackingMover = attackingAnchorWorld.OrderedEntities.Single(entity => entity.Id.Value == 2);
        var attackingShooterMovement = attackingShooter.Components.Require<MovementComponentState>();
        var attackingShooterWeapon = attackingShooter.Components.Require<WeaponUserComponentState>();
        var attackingCooldown = attackingShooterWeapon.Mounts.Max(mount => mount.CooldownRemaining);
        var attackingTargetDistance = attackingShooter.Transform.Position.DistanceTo(attackingAnchorWorld.OrderedEntities.Single(entity => entity.Id.Value == 3).Transform.Position);
        Assert(attackingShooterMovement.FireAnchorRemaining <= 0, "cooldown-gated attacker should prove combat-anchor, not fire-anchor protection");
        Assert(attackingShooter.Transform.Position.DistanceTo(attackingShooterStart) <= 0.01f, $"attacking anchor should not be displaced while waiting to fire; shooter {attackingShooter.Transform.Position}, mover {attackingMover.Transform.Position}, cooldown {attackingCooldown:0.00}, target distance {attackingTargetDistance:0.0}");
        Assert(attackingMover.Transform.Position.DistanceTo(attackingShooterStart) > 35f, "incoming mover should yield around an attacking anchor");
        Assert(attackingAnchorWorld.Metrics.AnchorPushEvents > 0, "combat-anchor separation should count anchor-push events");
        Console.WriteLine($"OK [attacking-anchor metric]: cooldown shooter held at {attackingShooter.Transform.Position}, mover yielded to {attackingMover.Transform.Position}.");
    }
}
