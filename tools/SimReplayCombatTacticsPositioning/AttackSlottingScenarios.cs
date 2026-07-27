static partial class Program
{
    static void RunAnchoredGroupAttackSlottingScenario()
    {
        // ---- Scenario 5: 30-unit group attack (ring, not center-stack) ---------------
        void AssertAttackSlotMathReservesAnchors()
        {
            var target = new Vector2(600, 450);
            const float weaponRange = 190f;
            const float targetRadius = 48f;
            var assignments = AttackSlotMath.AssignAttackSlots(
                [
                    new AttackSlotUnit(1, target + new Vector2(160, 0), weaponRange),
                    new AttackSlotUnit(2, target + new Vector2(-420, 0), weaponRange),
                ],
                target,
                targetRadius);

            var anchor = assignments.Single(assignment => assignment.Id == 1);
            var mover = assignments.Single(assignment => assignment.Id == 2);
            Assert(anchor.IsAnchor, "attack-slot math should keep the in-range unit as a firing anchor");
            Assert(!mover.IsAnchor, "attack-slot math should send the rear unit to a free slot");
            Assert(mover.Slot.X < target.X, $"attack-slot math should reserve the anchor bearing and assign rear slot behind target, got {mover.Slot}");
            Assert(mover.Slot.DistanceTo(anchor.Slot) > 250f, $"attack-slot math should keep mover slot away from anchor, got {mover.Slot.DistanceTo(anchor.Slot):0.0}px");
            Console.WriteLine($"OK [attack-slot math]: anchor at {anchor.Slot}, rear mover reserved {mover.Slot}.");
        }

        AssertAttackSlotMathReservesAnchors();

        var anchoredAttackTarget = new Vector2(600, 450);
        var anchoredAttackSubjects = new[] { new EntityId(1), new EntityId(2) };
        var anchoredAttackLog = new List<EntityCommand>
        {
            new GroupAttackEntityCommand(new OwnerId(1), anchoredAttackSubjects, 1, new EntityId(3), CombatTargetKind.Unit),
        };

        EntitySpec AttackSlotQaSpec(string id, EntityKind kind, IconGlyph glyph)
        {
            return new EntitySpec
            {
                Id = id,
                Kind = kind,
                Display = new EntityDisplaySpec(id, $"{id}.name", $"{id}.role", id.ToUpperInvariant()[..3], glyph),
                Stats = new StatsSpec(UnitWeightClass.Medium, ArmorTag.Vehicle, MaxHp: 1000, SightRange: 900, Cost: 100, TechTier: 1),
                Movement = kind == EntityKind.Unit ? new MovementSpec(MovementDomain.Land, Speed: 180, TurnRate: 8) : null,
                Collision = new CollisionSpec(Radius: kind == EntityKind.Unit ? 18 : 48, Mass: kind == EntityKind.Unit ? 1 : 100, PushPriority: kind == EntityKind.Unit ? 1 : 10),
                Weapons = kind == EntityKind.Unit
                    ?
                    [
                        WeaponMountSpec.Independent("main", WeaponIds.NeedleRifle, Vector2.Zero, new Vector2(14, 0), MathF.Tau, 12, fireWhileMoving: true),
                    ]
                    : [],
            };
        }

        EntityComponentState[] AttackSlotQaAttacker(Vector2 moveTarget)
        {
            return
            [
                new HealthComponentState(140, 140),
                new MovementComponentState(Vector2.Zero, moveTarget),
                new MovementProfileComponentState(MaxSpeed: 180, ArriveRadius: 4),
                new CollisionComponentState(Radius: 18, Mass: 1, PushPriority: 1, BlocksMovement: true),
                new VisionComponentState(900),
                new StanceComponentState(UnitStance.Aggressive),
                new WeaponUserComponentState(new[]
                {
                    new WeaponMountRuntimeState("main", WeaponIds.NeedleRifle, 0, 0),
                }),
            ];
        }

        EntityWorld BuildAnchoredGroupAttackSlotting()
        {
            var world = new EntityWorld(seed: 2411) { WorldWidth = 1200, WorldHeight = 900 };
            world.AddSystem(new CommandSystem());
            world.AddSystem(new VisionSystem());
            world.AddSystem(new CombatSystem());
            world.AddSystem(new ProjectileSystem());
            world.AddSystem(new MovementSystem());
            world.AddSystem(new SeparationSystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

            var attackerSpec = AttackSlotQaSpec("replay.slot_attacker", EntityKind.Unit, IconGlyph.Tank);
            var targetSpec = AttackSlotQaSpec("replay.slot_target", EntityKind.Unit, IconGlyph.Building);
            world.Spawn(attackerSpec, new OwnerId(1), EntityTransform.At(anchoredAttackTarget + new Vector2(160, 0)), AttackSlotQaAttacker(anchoredAttackTarget));
            world.Spawn(attackerSpec, new OwnerId(1), EntityTransform.At(anchoredAttackTarget + new Vector2(-420, 0)), AttackSlotQaAttacker(anchoredAttackTarget));
            world.Spawn(targetSpec, new OwnerId(2), EntityTransform.At(anchoredAttackTarget), new EntityComponentState[]
            {
                new HealthComponentState(100000, 100000),
                new CollisionComponentState(Radius: 48, Mass: 100, PushPriority: 10, BlocksMovement: true),
            });

            return world;
        }

        AssertDeterministic("anchored-group-attack-slotting", BuildAnchoredGroupAttackSlotting, anchoredAttackLog, 120, 20);

        var slotWorld = BuildAnchoredGroupAttackSlotting();
        var slotClock = new SimClock();
        var slotBuffer = new EntityCommandBuffer();
        foreach (var command in anchoredAttackLog)
        {
            slotBuffer.Enqueue(command);
        }

        slotWorld.Step(1, slotClock.FixedDelta, slotBuffer.DrainUpToTick(1));
        slotWorld.Events.Drain();
        var slotAnchor = slotWorld.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var slotMover = slotWorld.OrderedEntities.Single(entity => entity.Id.Value == 2);
        var anchorMovement = slotAnchor.Components.Require<MovementComponentState>();
        var moverMovement = slotMover.Components.Require<MovementComponentState>();
        Assert(anchorMovement.MoveTarget is null, "in-range attack anchor should hold instead of receiving a chase target");
        Assert(anchorMovement.FormationSlot is { } anchorSlot && anchorSlot.X > anchoredAttackTarget.X, $"attack anchor should reserve the right-side bearing, got {anchorMovement.FormationSlot}");
        Assert(moverMovement.FormationSlot is { } moverSlot && moverSlot.X < anchoredAttackTarget.X, $"rear attacker should receive the left-side reserved slot, got {moverMovement.FormationSlot}");
        Assert(moverMovement.MoveTarget is { } moverTarget && moverTarget.X < anchoredAttackTarget.X, $"CombatSystem should preserve the attack slot instead of overwriting with chase standoff, got {moverMovement.MoveTarget}");

        for (var tick = 2; tick <= 20; tick++)
        {
            slotWorld.Step(tick, slotClock.FixedDelta, slotBuffer.DrainUpToTick(tick));
            slotWorld.Events.Drain();
        }

        slotMover = slotWorld.OrderedEntities.Single(entity => entity.Id.Value == 2);
        moverMovement = slotMover.Components.Require<MovementComponentState>();
        Assert(moverMovement.MoveTarget is { } lateMoveTarget && lateMoveTarget.X < anchoredAttackTarget.X, $"rear attacker should keep its open-side attack slot while closing, got {moverMovement.MoveTarget}");
        Console.WriteLine($"OK [anchored-group-attack-slotting]: anchor held {slotAnchor.Transform.Position}, rear mover kept slot {moverMovement.MoveTarget}.");
    }
}
