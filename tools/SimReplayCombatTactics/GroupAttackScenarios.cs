static partial class Program
{
    static void RunGroupAttackScenario()
    {
        EntityWorld BuildGroupAttack()
        {
            var (world, ids) = BuildGroup(GroupSize, seed: 23);
            world.AddSystem(new CombatSystem());
            world.AddSystem(new ProjectileSystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

            // Give the grunts a weapon so they have a range ring.
            foreach (var id in ids)
            {
                world.TryGet(id, out var e);
                e.Components.Set(new VisionComponentState(900));
                e.Components.Set(new StanceComponentState(UnitStance.Aggressive));
                e.Components.Set(new WeaponUserComponentState(new[]
                {
                    new WeaponMountRuntimeState("main", WeaponKind.NeedleRifle, 0, 0),
                }));
            }

            // A tough stationary target at the center.
            var targetSpec = new EntitySpec
            {
                Id = "replay.bastion",
                Kind = EntityKind.Building,
                Display = new EntityDisplaySpec("Bastion", "b.name", "b.role", "BAS", IconGlyph.Building),
            };
            world.Spawn(targetSpec, new OwnerId(2), EntityTransform.At(GroupTarget), new EntityComponentState[]
            {
                new HealthComponentState(100000, 100000),
                new CollisionComponentState(Radius: 48, Mass: 100, PushPriority: 10, BlocksMovement: true),
            });

            return world;
        }

        // Target is entity id GroupSize+1 (spawned after the grunts).
        var bastionId = new EntityId(GroupSize + 1);
        var groupAttackLog = new List<EntityCommand>
        {
            new GroupAttackEntityCommand(new OwnerId(1), GroupIds, 1, bastionId, CombatTargetKind.Building),
        };

        AssertDeterministic("group-attack", BuildGroupAttack, groupAttackLog, AttackTicks, 250);

        // Metric: attackers must ring the target (not all at center). Check that final
        // attacker distances to the target cluster near the weapon-range ring.
        var ga = BuildGroupAttack();
        var gaClock = new SimClock();
        var gaBuffer = new EntityCommandBuffer();
        foreach (var c in groupAttackLog)
        {
            gaBuffer.Enqueue(c);
        }

        for (var tick = 1; tick <= AttackTicks; tick++)
        {
            ga.Step(tick, gaClock.FixedDelta, gaBuffer.DrainUpToTick(tick));
            ga.Events.Drain();
        }

        var attackers = EntityProjector.Project(ga).Where(p => p.Kind == EntityKind.Unit && p.Owner.Value == 1).ToList();
        var rifleRange = WeaponCatalog.Weapons[WeaponKind.NeedleRifle].Range;
        var tooClose = attackers.Count(p => p.Position.DistanceTo(GroupTarget) < 48f);
        var inBand = attackers.Count(p => p.Position.DistanceTo(GroupTarget) <= rifleRange + 64f);

        if (tooClose > 0)
        {
            Fail($"group-attack stacked on center: {tooClose} attackers inside the target footprint");
        }

        if (inBand < attackers.Count * 0.8f)
        {
            Fail($"group-attack failed to reach firing band: only {inBand}/{attackers.Count} within range+slack");
        }

        if (ga.Metrics.TargetSwitchCount != 0)
        {
            Fail($"group-attack target flicker: {ga.Metrics.TargetSwitchCount} target switches");
        }

        Console.WriteLine($"OK [group-attack metric]: {attackers.Count} attackers ringed target, 0 center-stacked, {inBand} in firing band (range {rifleRange:0}).");
    }
}
