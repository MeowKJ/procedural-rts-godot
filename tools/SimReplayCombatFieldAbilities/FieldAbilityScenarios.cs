static partial class Program
{
    static void AssertRepairFieldAbility()
    {
        const int ticks = 50;

        EntitySpec TargetSpec(string id)
        {
            return new EntitySpec
            {
                Id = id,
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Repair Target", "repair.name", "repair.role", "RPR", IconGlyph.Infantry),
            };
        }

        EntityWorld BuildRepairWorld()
        {
            var world = new EntityWorld(seed: 9191);
            world.AddSystem(new AbilitySystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

            world.SpawnUnit(UnitDesignCatalog.Spec("dog.engineer"), new OwnerId(1), Vector2.Zero);
            world.Spawn(TargetSpec("replay.repair_ally"), new OwnerId(1), EntityTransform.At(new Vector2(20, 0)), new EntityComponentState[]
            {
                new HealthComponentState(50, 100),
            });
            world.Spawn(TargetSpec("replay.repair_enemy"), new OwnerId(2), EntityTransform.At(new Vector2(25, 0)), new EntityComponentState[]
            {
                new HealthComponentState(40, 100),
            });
            world.Spawn(TargetSpec("replay.repair_far_ally"), new OwnerId(1), EntityTransform.At(new Vector2(250, 0)), new EntityComponentState[]
            {
                new HealthComponentState(50, 100),
            });

            return world;
        }

        var repairCommands = new List<EntityCommand>
        {
            new AbilityEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 1, AbilityKind.RepairField, new EntityId(2)),
            new AbilityEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 2, AbilityKind.RepairField, new EntityId(2)),
            new AbilityEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 40, AbilityKind.RepairField, new EntityId(2)),
        };

        AssertDeterministic("repair-field", BuildRepairWorld, repairCommands, ticks, 10);

        var world = BuildRepairWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        foreach (var command in repairCommands)
        {
            buffer.Enqueue(command);
        }

        for (var tick = 1; tick <= ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
        }

        var engineer = world.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var ally = world.OrderedEntities.Single(entity => entity.Id.Value == 2).Components.Require<HealthComponentState>();
        var enemy = world.OrderedEntities.Single(entity => entity.Id.Value == 3).Components.Require<HealthComponentState>();
        var farAlly = world.OrderedEntities.Single(entity => entity.Id.Value == 4).Components.Require<HealthComponentState>();
        var cooldown = engineer.Components.Require<AbilityRuntimeComponentState>()
            .Cooldowns.Single(state => state.Kind == AbilityKind.RepairField)
            .CooldownRemaining;

        Assert(Math.Abs(ally.Hp - 82) < 0.001f, $"repair field should heal damaged ally twice after cooldown, got {ally.Hp}");
        Assert(Math.Abs(enemy.Hp - 40) < 0.001f, $"repair field should not heal hostile units, got {enemy.Hp}");
        Assert(Math.Abs(farAlly.Hp - 50) < 0.001f, $"repair field should not heal allies outside radius, got {farAlly.Hp}");
        Assert(cooldown > 0, "repair field should leave a cooldown after the final successful cast");
        Console.WriteLine($"OK [repair-field]: ally hp {ally.Hp}, enemy hp {enemy.Hp}, far ally hp {farAlly.Hp}, cooldown {cooldown:0.00}s.");
    }

    static void AssertShieldFieldAbility()
    {
        const int ticks = 70;

        EntitySpec ShieldCasterSpec()
        {
            return new EntitySpec
            {
                Id = "replay.shield_caster",
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Shield Caster", "shield.name", "shield.role", "SHD", IconGlyph.StanceHold),
                Abilities =
                [
                    new AbilitySpec(AbilityKind.ShieldField, Radius: 90, Value: 18),
                ],
            };
        }

        EntitySpec ShieldTargetSpec(string id)
        {
            return new EntitySpec
            {
                Id = id,
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Shield Target", "shield.target.name", "shield.target.role", "TGT", IconGlyph.Infantry),
                Stats = new StatsSpec(UnitWeightClass.Medium, ArmorTag.Vehicle, MaxHp: 100, SightRange: 100, Cost: 50, TechTier: 1),
                Movement = new MovementSpec(MovementDomain.Land, Speed: 0, TurnRate: 0),
            };
        }

        EntitySpec ShooterSpec()
        {
            return new EntitySpec
            {
                Id = "replay.shield_shooter",
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Shooter", "shooter.name", "shooter.role", "SHT", IconGlyph.AttackMove),
                Stats = new StatsSpec(UnitWeightClass.Medium, ArmorTag.Vehicle, MaxHp: 100, SightRange: 320, Cost: 50, TechTier: 1),
                Movement = new MovementSpec(MovementDomain.Land, Speed: 0, TurnRate: 0),
                Weapons =
                [
                    WeaponMountSpec.Omni("main", WeaponKind.NeedleRifle, Vector2.Zero, fireWhileMoving: false),
                ],
            };
        }

        EntityWorld BuildShieldWorld()
        {
            var world = new EntityWorld(seed: 9292);
            world.AddSystem(new AbilitySystem());
            world.AddSystem(new CombatSystem());
            world.AddSystem(new ProjectileSystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

            world.Spawn(ShieldCasterSpec(), new OwnerId(1), EntityTransform.At(Vector2.Zero), new EntityComponentState[]
            {
                new HealthComponentState(100, 100),
                new AbilityRuntimeComponentState(new[]
                {
                    new AbilityCooldownState(AbilityKind.ShieldField, 0),
                }),
            });
            world.Spawn(ShieldTargetSpec("replay.shield_ally"), new OwnerId(1), EntityTransform.At(new Vector2(40, 0)), new EntityComponentState[]
            {
                new HealthComponentState(100, 100),
            });
            world.Spawn(ShieldTargetSpec("replay.shield_enemy"), new OwnerId(2), EntityTransform.At(new Vector2(45, 0)), new EntityComponentState[]
            {
                new HealthComponentState(100, 100),
            });
            world.Spawn(ShieldTargetSpec("replay.shield_far_ally"), new OwnerId(1), EntityTransform.At(new Vector2(220, 0)), new EntityComponentState[]
            {
                new HealthComponentState(100, 100),
            });
            world.Spawn(ShooterSpec(), new OwnerId(2), EntityTransform.At(new Vector2(175, 0)), new EntityComponentState[]
            {
                new HealthComponentState(100, 100),
                new VisionComponentState(320),
                new StanceComponentState(UnitStance.Hold),
                new WeaponUserComponentState(new[]
                {
                    new WeaponMountRuntimeState("main", WeaponKind.NeedleRifle, 0, 0),
                }, new EntityId(2), CombatTargetKind.Unit, AttackTargetIsManual: true),
            });

            return world;
        }

        var shieldCommands = new List<EntityCommand>
        {
            new AbilityEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 1, AbilityKind.ShieldField, new EntityId(2)),
        };
        AssertDeterministic("shield-field", BuildShieldWorld, shieldCommands, ticks, 10);

        var shielded = RunShieldWorld(BuildShieldWorld(), shieldCommands, ticks);
        var unshielded = RunShieldWorld(BuildShieldWorld(), Array.Empty<EntityCommand>(), ticks);
        Assert(shielded.AllyHp > unshielded.AllyHp + 10, $"shield should reduce real combat HP loss, shielded {shielded.AllyHp}, unshielded {unshielded.AllyHp}");
        Assert(shielded.EnemyHasShield == false, "shield field should not apply to hostile units in radius");
        Assert(shielded.FarAllyHasShield == false, "shield field should not apply outside radius");
        Assert(shielded.Cooldown > 0, "shield field should leave a cooldown after successful cast");
        Assert(shielded.Shots > 0 && Math.Abs(shielded.Shots - unshielded.Shots) < 0.001f, "shield scenario should compare the same number of incoming shots");
        Console.WriteLine($"OK [shield-field]: shielded ally hp {shielded.AllyHp:0.0} > unshielded {unshielded.AllyHp:0.0}, shots {shielded.Shots}.");
    }

    static (float AllyHp, bool EnemyHasShield, bool FarAllyHasShield, float Cooldown, int Shots) RunShieldWorld(
        EntityWorld world,
        IReadOnlyList<EntityCommand> commands,
        int ticks)
    {
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        foreach (var command in commands)
        {
            buffer.Enqueue(command);
        }

        var shots = 0;
        for (var tick = 1; tick <= ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
            shots += world.Events.Drain().Count(evt => evt is WeaponFiredEvent);
        }

        var caster = world.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var ally = world.OrderedEntities.Single(entity => entity.Id.Value == 2);
        var enemy = world.OrderedEntities.Single(entity => entity.Id.Value == 3);
        var farAlly = world.OrderedEntities.Single(entity => entity.Id.Value == 4);
        return (
            ally.Components.Require<HealthComponentState>().Hp,
            enemy.Components.Has<ShieldComponentState>(),
            farAlly.Components.Has<ShieldComponentState>(),
            caster.Components.Require<AbilityRuntimeComponentState>().Cooldowns.Single(state => state.Kind == AbilityKind.ShieldField).CooldownRemaining,
            shots);
    }

    static void AssertScanAbility()
    {
        const int ticks = 70;

        EntitySpec ScannerSpec()
        {
            return new EntitySpec
            {
                Id = "replay.scanner",
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Scanner", "scan.name", "scan.role", "SCN", IconGlyph.Settings),
                Abilities =
                [
                    new AbilitySpec(AbilityKind.Scan, Radius: 160, Value: 1.2f),
                ],
            };
        }

        EntitySpec TargetSpec(string id)
        {
            return new EntitySpec
            {
                Id = id,
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Scan Target", "scan.target.name", "scan.target.role", "TGT", IconGlyph.Infantry),
            };
        }

        EntityWorld BuildScanWorld()
        {
            var world = new EntityWorld(seed: 9393);
            world.AddSystem(new AbilitySystem());
            world.AddSystem(new VisionSystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

            world.Spawn(ScannerSpec(), new OwnerId(1), EntityTransform.At(Vector2.Zero), new EntityComponentState[]
            {
                new HealthComponentState(100, 100),
                new AbilityRuntimeComponentState(new[]
                {
                    new AbilityCooldownState(AbilityKind.Scan, 0),
                }),
            });
            world.Spawn(TargetSpec("replay.scan_enemy"), new OwnerId(2), EntityTransform.At(new Vector2(420, 0)), new EntityComponentState[]
            {
                new HealthComponentState(100, 100),
            });
            world.Spawn(TargetSpec("replay.scan_far_enemy"), new OwnerId(2), EntityTransform.At(new Vector2(700, 0)), new EntityComponentState[]
            {
                new HealthComponentState(100, 100),
            });

            return world;
        }

        var scanCommands = new List<EntityCommand>
        {
            new AbilityEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 1, AbilityKind.Scan, TargetPoint: new Vector2(420, 0)),
        };
        AssertDeterministic("scan", BuildScanWorld, scanCommands, ticks, 10);

        var world = BuildScanWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        foreach (var command in scanCommands)
        {
            buffer.Enqueue(command);
        }

        var enemyVisibleDuringScan = false;
        var farEnemyVisibleDuringScan = false;
        var enemyVisibleAfterExpiry = true;
        for (var tick = 1; tick <= ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
            if (tick == 2)
            {
                enemyVisibleDuringScan = world.Visibility.IsVisible(new OwnerId(1), new EntityId(2));
                farEnemyVisibleDuringScan = world.Visibility.IsVisible(new OwnerId(1), new EntityId(3));
            }

            if (tick == ticks)
            {
                enemyVisibleAfterExpiry = world.Visibility.IsVisible(new OwnerId(1), new EntityId(2));
            }
        }

        var scanner = world.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var scanEffectsRemaining = world.OrderedEntities.Count(entity => entity.Components.Has<ScanRevealComponentState>());
        var cooldown = scanner.Components.Require<AbilityRuntimeComponentState>()
            .Cooldowns.Single(state => state.Kind == AbilityKind.Scan)
            .CooldownRemaining;

        Assert(enemyVisibleDuringScan, "scan should reveal a hostile target inside its radius");
        Assert(!farEnemyVisibleDuringScan, "scan should not reveal hostiles outside its radius");
        Assert(!enemyVisibleAfterExpiry, "scan reveal should expire and stop revealing the target");
        Assert(scanEffectsRemaining == 0, $"expired scan reveal effect should be removed, got {scanEffectsRemaining}");
        Assert(cooldown > 0, "scan should leave a cooldown after successful cast");
        Console.WriteLine($"OK [scan]: hostile visible during scan, far hostile hidden, expired effects {scanEffectsRemaining}, cooldown {cooldown:0.00}s.");
    }
}
