static partial class Program
{
    static void AssertPatrolCommandCore()
    {
        const int ticks = 1000;
        var pointA = new Vector2(100, 500);
        var pointB = new Vector2(700, 500);
        var owner = new OwnerId(1);
        var patrolSubject = new[] { new EntityId(1) };

        EntitySpec PatrolUnitSpec()
        {
            return new EntitySpec
            {
                Id = "replay.patrol_unit",
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Patrol Unit", "patrol.unit.name", "patrol.unit.role", "PAT", IconGlyph.Move),
                Stats = new StatsSpec(UnitWeightClass.Medium, ArmorTag.Vehicle, MaxHp: 400, SightRange: 260, Cost: 100, TechTier: 1),
                Movement = new MovementSpec(MovementDomain.Land, Speed: 180, TurnRate: 8),
                Collision = new CollisionSpec(Radius: 12, Mass: 1, PushPriority: 1),
                Weapons =
                [
                    WeaponMountSpec.Independent("main", WeaponKind.NeedleRifle, Vector2.Zero, new Vector2(12, 0), MathF.Tau, 8, fireWhileMoving: true),
                ],
            };
        }

        EntitySpec PatrolThreatSpec()
        {
            return new EntitySpec
            {
                Id = "replay.patrol_threat",
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Patrol Threat", "patrol.threat.name", "patrol.threat.role", "THR", IconGlyph.Infantry),
            };
        }

        EntityWorld BuildPatrolWorld(bool includeThreat = true)
        {
            var world = new EntityWorld(seed: 9797)
            {
                WorldWidth = 900,
                WorldHeight = 900,
            };
            world.AddSystem(new CommandSystem());
            world.AddSystem(new VisionSystem());
            world.AddSystem(new CombatSystem());
            world.AddSystem(new ProjectileSystem());
            world.AddSystem(new MovementSystem());
            world.Relations.Set(owner, new OwnerId(2), PlayerRelation.Hostile);

            var unitSpec = PatrolUnitSpec();
            world.Spawn(unitSpec, owner, EntityTransform.At(pointA), new EntityComponentState[]
            {
                new HealthComponentState(400, 400),
                new CommandableComponentState(),
                new MovementComponentState(Vector2.Zero),
                new MovementProfileComponentState(MaxSpeed: 180, ArriveRadius: 2),
                new CollisionComponentState(12, 1, 1, true),
                new VisionComponentState(260),
                new StanceComponentState(UnitStance.Aggressive, pointA),
                new AutonomyComponentState(AcquireRange: 260, LeashRange: 800, AnchorPosition: pointA),
                new WeaponUserComponentState(new[]
                {
                    new WeaponMountRuntimeState("main", WeaponKind.NeedleRifle, 0, 0),
                }),
            });

            if (includeThreat)
            {
                world.Spawn(PatrolThreatSpec(), new OwnerId(2), EntityTransform.At(new Vector2(430, 500)), new EntityComponentState[]
                {
                    new HealthComponentState(12, 12),
                    new CollisionComponentState(12, 1, 1, true),
                });
            }

            return world;
        }

        var patrolLog = new List<EntityCommand>
        {
            new PatrolEntityCommand(owner, patrolSubject, 1, pointA, pointB),
        };
        AssertDeterministic("patrol", () => BuildPatrolWorld(), patrolLog, ticks, 50);

        var world = BuildPatrolWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        foreach (var command in patrolLog)
        {
            buffer.Enqueue(command);
        }

        var sawCombatTarget = false;
        var threatDestroyed = false;
        var resumedAfterThreat = false;
        var sawPointB = false;
        var sawPointAAfterB = false;

        for (var tick = 1; tick <= ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
            var drained = world.Events.Drain();
            threatDestroyed |= drained.Any(evt => evt is EntityDestroyedEvent destroyed && destroyed.Entity.Value == 2);

            var unit = world.OrderedEntities.Single(entity => entity.Id.Value == 1);
            var weapon = unit.Components.Require<WeaponUserComponentState>();
            sawCombatTarget |= weapon.AttackTarget.Value == 2;

            if (unit.Components.TryGet<PatrolOrderComponentState>(out var patrol))
            {
                sawPointB |= !patrol.MovingToB;
                sawPointAAfterB |= sawPointB
                    && patrol.MovingToB
                    && unit.Transform.Position.DistanceSquaredTo(pointA) <= 4f;

                if (threatDestroyed
                    && !weapon.AttackTarget.IsValid
                    && unit.Components.TryGet<MovementComponentState>(out var movement)
                    && IsPatrolRouteTarget(unit, movement, patrol))
                {
                    resumedAfterThreat = true;
                }
            }
        }

        Assert(sawCombatTarget, "Patrol should auto-engage a hostile encountered along the route");
        Assert(threatDestroyed, "Patrol should destroy the route threat through CombatSystem");
        Assert(resumedAfterThreat, "Patrol should resume route after destroying a threat");
        Assert(sawPointB, "Patrol should flip from B back toward A");
        Assert(sawPointAAfterB, "Patrol should complete an A->B->A cycle");

        AssertPatrolClearedByExplicitCommands();

        Console.WriteLine("OK [patrol]: unit looped A->B->A, auto-engaged route threat, and resumed patrol intent.");

        static bool IsPatrolRouteTarget(EntityInstance unit, MovementComponentState movement, PatrolOrderComponentState patrol)
        {
            var target = patrol.MovingToB ? patrol.PointB : patrol.PointA;
            if (movement.MoveTarget is { } moveTarget && moveTarget.DistanceSquaredTo(target) <= 1f)
            {
                return true;
            }

            return movement.MoveTarget is not null
                && unit.Components.TryGet<PathfindingComponentState>(out var path)
                && new Vector2(path.Goal.X, path.Goal.Y).DistanceSquaredTo(target) <= 1f;
        }

        void AssertPatrolClearedByExplicitCommands()
        {
            var moveWorld = RunPatrolOverride(
                new MoveEntityCommand(owner, patrolSubject, 2, new Vector2(220, 680), MoveCommandMode.Direct),
                includeThreat: false);
            var moved = moveWorld.OrderedEntities.Single(entity => entity.Id.Value == 1);
            Assert(!moved.Components.Has<PatrolOrderComponentState>(), "explicit Move should clear Patrol");
            Assert(moved.Components.Require<CommandableComponentState>().MoveMode == MoveCommandMode.Direct, "explicit Move should restore direct move semantics");

            var attackWorld = RunPatrolOverride(
                new AttackEntityCommand(owner, patrolSubject, 2, new EntityId(2), CombatTargetKind.Unit),
                includeThreat: true);
            var attacker = attackWorld.OrderedEntities.Single(entity => entity.Id.Value == 1);
            var attackWeapon = attacker.Components.Require<WeaponUserComponentState>();
            Assert(!attacker.Components.Has<PatrolOrderComponentState>(), "explicit Attack should clear Patrol");
            Assert(attackWeapon.AttackTarget.Value == 2 && attackWeapon.AttackTargetIsManual, "explicit Attack should remain a manual focus command");

            var stopWorld = RunPatrolOverride(
                new StopEntityCommand(owner, patrolSubject, 2),
                includeThreat: false);
            var stopped = stopWorld.OrderedEntities.Single(entity => entity.Id.Value == 1);
            Assert(!stopped.Components.Has<PatrolOrderComponentState>(), "Stop should clear Patrol");
            Assert(stopped.Components.Require<MovementComponentState>().MoveTarget is null, "Stop should clear Patrol movement target");
        }

        EntityWorld RunPatrolOverride(EntityCommand explicitCommand, bool includeThreat)
        {
            var overrideWorld = BuildPatrolWorld(includeThreat);
            var overrideClock = new SimClock();
            var overrideBuffer = new EntityCommandBuffer();
            overrideBuffer.Enqueue(new PatrolEntityCommand(owner, patrolSubject, 1, pointA, pointB));
            overrideBuffer.Enqueue(explicitCommand);

            for (var tick = 1; tick <= 3; tick++)
            {
                overrideWorld.Step(tick, overrideClock.FixedDelta, overrideBuffer.DrainUpToTick(tick));
                overrideWorld.Events.Drain();
            }

            return overrideWorld;
        }
    }
}
