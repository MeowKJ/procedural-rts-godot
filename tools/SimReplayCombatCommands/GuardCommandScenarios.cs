static partial class Program
{
    static void AssertGuardCommandCore()
    {
        const int ticks = 620;
        var owner = new OwnerId(1);
        var enemy = new OwnerId(2);
        var entityGuardStart = new Vector2(120, 400);
        var guardedStart = new Vector2(520, 400);
        var guardedMoveTarget = new Vector2(700, 400);
        var entityGuardRadius = 150f;
        var areaGuardPoint = new Vector2(280, 650);
        var areaGuardRadius = 160f;
        var entityGuardSubject = new[] { new EntityId(1) };
        var protectedSubject = new[] { new EntityId(2) };
        var areaGuardSubject = new[] { new EntityId(4) };

        EntitySpec GuardUnitSpec(string id, string label)
        {
            return new EntitySpec
            {
                Id = id,
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec(label, id + ".name", id + ".role", "GRD", IconGlyph.StanceReturn),
                Stats = new StatsSpec(UnitWeightClass.Light, ArmorTag.Infantry, MaxHp: 180, SightRange: 320, Cost: 100, TechTier: 1),
                Movement = new MovementSpec(MovementDomain.Land, Speed: 210, TurnRate: 8),
                Collision = new CollisionSpec(Radius: 11, Mass: 1, PushPriority: 1),
                Weapons =
                [
                    WeaponMountSpec.Independent("main", WeaponKind.NeedleRifle, Vector2.Zero, new Vector2(12, 0), MathF.Tau, 8, fireWhileMoving: true),
                ],
                Abilities =
                [
                    new AbilitySpec(AbilityKind.RepairField, Radius: 48, Value: 20),
                ],
            };
        }

        EntitySpec ProtectedSpec()
        {
            return new EntitySpec
            {
                Id = "replay.guard_protected",
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Protected Unit", "guard.protected.name", "guard.protected.role", "PRT", IconGlyph.Infantry),
                Movement = new MovementSpec(MovementDomain.Land, Speed: 180, TurnRate: 8),
                Collision = new CollisionSpec(Radius: 12, Mass: 1, PushPriority: 1),
            };
        }

        EntitySpec HostileSpec(string id, string label, bool armed)
        {
            return new EntitySpec
            {
                Id = id,
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec(label, id + ".name", id + ".role", "HST", IconGlyph.Infantry),
                Stats = new StatsSpec(UnitWeightClass.Light, ArmorTag.Infantry, MaxHp: 28, SightRange: 260, Cost: 80, TechTier: 1),
                Collision = new CollisionSpec(Radius: 11, Mass: 1, PushPriority: 1),
                Weapons = armed
                    ?
                    [
                        WeaponMountSpec.Independent("main", WeaponKind.NeedleRifle, Vector2.Zero, new Vector2(12, 0), MathF.Tau, 8, fireWhileMoving: true),
                    ]
                    : [],
            };
        }

        EntityWorld BuildGuardWorld()
        {
            var world = new EntityWorld(seed: 9898)
            {
                WorldWidth = 900,
                WorldHeight = 900,
            };
            world.AddSystem(new CommandSystem());
            world.AddSystem(new VisionSystem());
            world.AddSystem(new CombatSystem());
            world.AddSystem(new ProjectileSystem());
            world.AddSystem(new MovementSystem());
            world.Relations.Set(owner, enemy, PlayerRelation.Hostile);

            world.Spawn(GuardUnitSpec("replay.guard_entity_unit", "Entity Guard"), owner, EntityTransform.At(entityGuardStart), GuardComponents(entityGuardStart));
            world.Spawn(ProtectedSpec(), owner, EntityTransform.At(guardedStart), new EntityComponentState[]
            {
                new HealthComponentState(180, 180),
                new CommandableComponentState(),
                new MovementComponentState(Vector2.Zero),
                new MovementProfileComponentState(MaxSpeed: 180, ArriveRadius: 2),
                new CollisionComponentState(12, 1, 1, true),
                new VisionComponentState(300),
            });
            world.Spawn(HostileSpec("replay.guard_entity_threat", "Entity Threat", armed: true), enemy, EntityTransform.At(new Vector2(560, 400)), new EntityComponentState[]
            {
                new HealthComponentState(28, 28),
                new CollisionComponentState(11, 1, 1, true),
                new VisionComponentState(260),
                new WeaponUserComponentState(new[]
                {
                    new WeaponMountRuntimeState("main", WeaponKind.NeedleRifle, 0, 0),
                }),
            });
            world.Spawn(GuardUnitSpec("replay.guard_area_unit", "Area Guard"), owner, EntityTransform.At(new Vector2(240, 650)), GuardComponents(new Vector2(240, 650)));
            world.Spawn(HostileSpec("replay.guard_area_intruder", "Area Intruder", armed: false), enemy, EntityTransform.At(new Vector2(330, 650)), new EntityComponentState[]
            {
                new HealthComponentState(22, 22),
                new CollisionComponentState(11, 1, 1, true),
            });
            world.Spawn(new EntitySpec
            {
                Id = "replay.guard_resource",
                Kind = EntityKind.Resource,
                Display = new EntityDisplaySpec("Guard Resource", "guard.resource.name", "guard.resource.role", "RES", IconGlyph.Credits),
            }, OwnerId.None, EntityTransform.At(new Vector2(760, 660)), new EntityComponentState[]
            {
                new ResourceNodeComponentState(Amount: 100, MaxAmount: 100),
            });
            world.Spawn(ProtectedSpec() with
            {
                Id = "replay.guard_repair_target",
                Display = new EntityDisplaySpec("Guard Repair Target", "guard.repair.name", "guard.repair.role", "RPR", IconGlyph.Settings),
            }, owner, EntityTransform.At(new Vector2(620, 470)), new EntityComponentState[]
            {
                new HealthComponentState(40, 100),
                new CollisionComponentState(12, 1, 1, true),
            });

            return world;
        }

        EntityComponentState[] GuardComponents(Vector2 anchor)
        {
            return
            [
                new HealthComponentState(180, 180),
                new CommandableComponentState(),
                new MovementComponentState(Vector2.Zero),
                new MovementProfileComponentState(MaxSpeed: 210, ArriveRadius: 2),
                new CollisionComponentState(11, 1, 1, true),
                new VisionComponentState(320),
                new StanceComponentState(UnitStance.Aggressive, anchor),
                new AutonomyComponentState(AcquireRange: 320, LeashRange: 420, AnchorPosition: anchor),
                new HarvesterComponentState(),
                new ResourceCargoComponentState(Cargo: 0, Capacity: 40),
                new WeaponUserComponentState(new[]
                {
                    new WeaponMountRuntimeState("main", WeaponKind.NeedleRifle, 0, 0),
                }),
            ];
        }

        var guardLog = new List<EntityCommand>
        {
            new GuardEntityCommand(owner, entityGuardSubject, 1, guardedStart, entityGuardRadius, new EntityId(2)),
            new AttackEntityCommand(enemy, new[] { new EntityId(3) }, 1, new EntityId(2), CombatTargetKind.Unit),
            new GuardEntityCommand(owner, areaGuardSubject, 1, areaGuardPoint, areaGuardRadius),
            new MoveEntityCommand(owner, protectedSubject, 260, guardedMoveTarget, MoveCommandMode.Direct),
        };
        AssertDeterministic("guard", BuildGuardWorld, guardLog, ticks, 40);

        var world = BuildGuardWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        foreach (var command in guardLog)
        {
            buffer.Enqueue(command);
        }

        var entityGuardOrderApplied = false;
        var entityGuardMovedTowardAlly = false;
        var entityGuardAttackedThreat = false;
        var entityThreatDestroyed = false;
        var entityGuardReturned = false;
        var entityGuardFollowedMovedTarget = false;
        var areaGuardOrderApplied = false;
        var areaGuardAttackedThreat = false;
        var areaThreatDestroyed = false;

        for (var tick = 1; tick <= ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
            var drained = world.Events.Drain();
            entityThreatDestroyed |= drained.Any(evt => evt is EntityDestroyedEvent destroyed && destroyed.Entity.Value == 3);
            areaThreatDestroyed |= drained.Any(evt => evt is EntityDestroyedEvent destroyed && destroyed.Entity.Value == 5);

            var entityGuard = world.OrderedEntities.Single(entity => entity.Id.Value == 1);
            var protectedAlly = world.OrderedEntities.Single(entity => entity.Id.Value == 2);
            var entityWeapon = entityGuard.Components.Require<WeaponUserComponentState>();
            entityGuardAttackedThreat |= entityWeapon.AttackTarget.Value == 3;
            entityGuardMovedTowardAlly |= entityGuard.Transform.Position.DistanceSquaredTo(guardedStart)
                < entityGuardStart.DistanceSquaredTo(guardedStart);

            if (entityGuard.Components.TryGet<GuardOrderComponentState>(out var entityGuardOrder))
            {
                entityGuardOrderApplied |= entityGuardOrder.TargetEntity.Value == 2
                    && entityGuardOrder.GuardPoint.DistanceSquaredTo(guardedStart) <= 1f
                    && MathF.Abs(entityGuardOrder.Radius - entityGuardRadius) <= 0.001f;
            }

            if (entityThreatDestroyed
                && tick < 260
                && !entityWeapon.AttackTarget.IsValid
                && entityGuard.Components.TryGet<MovementComponentState>(out var returnedMovement)
                && returnedMovement.MoveTarget is null
                && entityGuard.Transform.Position.DistanceSquaredTo(protectedAlly.Transform.Position) <= entityGuardRadius * entityGuardRadius)
            {
                entityGuardReturned = true;
            }

            if (tick > 260
                && protectedAlly.Transform.Position.DistanceSquaredTo(guardedMoveTarget) <= 4f
                && entityGuard.Transform.Position.DistanceSquaredTo(protectedAlly.Transform.Position) <= entityGuardRadius * entityGuardRadius)
            {
                entityGuardFollowedMovedTarget = true;
            }

            var areaGuard = world.OrderedEntities.Single(entity => entity.Id.Value == 4);
            var areaWeapon = areaGuard.Components.Require<WeaponUserComponentState>();
            areaGuardAttackedThreat |= areaWeapon.AttackTarget.Value == 5;
            if (areaGuard.Components.TryGet<GuardOrderComponentState>(out var areaGuardOrder))
            {
                areaGuardOrderApplied |= !areaGuardOrder.TargetEntity.IsValid
                    && areaGuardOrder.GuardPoint.DistanceSquaredTo(areaGuardPoint) <= 1f
                    && MathF.Abs(areaGuardOrder.Radius - areaGuardRadius) <= 0.001f;
            }
        }

        Assert(entityGuardOrderApplied, "Guard should store protected entity, guard point, and radius");
        Assert(entityGuardMovedTowardAlly, "Guard should follow toward a protected friendly outside guard range");
        Assert(entityGuardAttackedThreat, "Guard should attack a hostile threatening the protected entity");
        Assert(entityThreatDestroyed, "Guard should destroy the protected entity threat");
        Assert(entityGuardReturned, "Guard should return to guard intent after the protected threat clears");
        Assert(entityGuardFollowedMovedTarget, "Guard should follow a moving protected entity after combat");
        Assert(areaGuardOrderApplied, "Guard should support fixed point/area orders without a target entity");
        Assert(areaGuardAttackedThreat, "Area Guard should attack an enemy inside the guarded area");
        Assert(areaThreatDestroyed, "Area Guard should destroy the guarded-area intruder");

        AssertGuardClearedByExplicitCommands();

        Console.WriteLine("OK [guard]: entity guard protected/followed an ally, area guard held a point, threats were cleared, and explicit orders cleared Guard.");

        void AssertGuardClearedByExplicitCommands()
        {
            var moveWorld = RunGuardOverride(
                new MoveEntityCommand(owner, entityGuardSubject, 2, new Vector2(260, 520), MoveCommandMode.Direct));
            var moved = EntityGuard(moveWorld);
            Assert(!moved.Components.Has<GuardOrderComponentState>(), "explicit Move should clear Guard");
            Assert(moved.Components.Require<CommandableComponentState>().MoveMode == MoveCommandMode.Direct, "explicit Move should restore direct move semantics after Guard");

            var attackWorld = RunGuardOverride(
                new AttackEntityCommand(owner, entityGuardSubject, 2, new EntityId(3), CombatTargetKind.Unit));
            var attacker = EntityGuard(attackWorld);
            var attackWeapon = attacker.Components.Require<WeaponUserComponentState>();
            Assert(!attacker.Components.Has<GuardOrderComponentState>(), "explicit Attack should clear Guard");
            Assert(attackWeapon.AttackTarget.Value == 3 && attackWeapon.AttackTargetIsManual, "explicit Attack should remain manual after clearing Guard");

            var stopWorld = RunGuardOverride(new StopEntityCommand(owner, entityGuardSubject, 2));
            var stopped = EntityGuard(stopWorld);
            Assert(!stopped.Components.Has<GuardOrderComponentState>(), "Stop should clear Guard");
            Assert(stopped.Components.Require<MovementComponentState>().MoveTarget is null, "Stop should clear Guard movement target");

            var holdWorld = RunGuardOverride(new HoldPositionEntityCommand(owner, entityGuardSubject, 2));
            var held = EntityGuard(holdWorld);
            Assert(!held.Components.Has<GuardOrderComponentState>(), "Hold should clear Guard");
            Assert(held.Components.Require<StanceComponentState>().Stance == UnitStance.Hold, "Hold should keep hold-position stance after clearing Guard");

            var harvestWorld = RunGuardOverride(new HarvestEntityCommand(owner, entityGuardSubject, 2, new EntityId(6)));
            var harvester = EntityGuard(harvestWorld);
            Assert(!harvester.Components.Has<GuardOrderComponentState>(), "Harvest should clear Guard");
            Assert(harvester.Components.Require<HarvesterComponentState>().Mode == HarvesterMode.MovingToField, "Harvest should keep harvest intent after clearing Guard");

            var repairWorld = RunGuardOverride(new RepairEntityCommand(owner, entityGuardSubject, 2, new EntityId(7)));
            var repairer = EntityGuard(repairWorld);
            Assert(!repairer.Components.Has<GuardOrderComponentState>(), "Repair should clear Guard");
            Assert(repairer.Components.Has<RepairOrderComponentState>(), "Repair should keep repair order after clearing Guard");

            var patrolWorld = RunGuardOverride(new PatrolEntityCommand(owner, entityGuardSubject, 2, entityGuardStart, new Vector2(340, 400)));
            var patrol = EntityGuard(patrolWorld);
            Assert(!patrol.Components.Has<GuardOrderComponentState>(), "Patrol should clear Guard");
            Assert(patrol.Components.Has<PatrolOrderComponentState>(), "Patrol should keep patrol order after clearing Guard");
        }

        EntityWorld RunGuardOverride(EntityCommand explicitCommand)
        {
            var overrideWorld = BuildGuardWorld();
            var overrideClock = new SimClock();
            var overrideBuffer = new EntityCommandBuffer();
            overrideBuffer.Enqueue(new GuardEntityCommand(owner, entityGuardSubject, 1, guardedStart, entityGuardRadius, new EntityId(2)));
            overrideBuffer.Enqueue(explicitCommand);

            for (var tick = 1; tick <= 4; tick++)
            {
                overrideWorld.Step(tick, overrideClock.FixedDelta, overrideBuffer.DrainUpToTick(tick));
                overrideWorld.Events.Drain();
            }

            return overrideWorld;
        }

        static EntityInstance EntityGuard(EntityWorld world)
        {
            return world.OrderedEntities.Single(entity => entity.Id.Value == 1);
        }
    }
}
