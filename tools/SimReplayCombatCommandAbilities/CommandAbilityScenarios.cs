static partial class Program
{
    static void AssertDeployAbility()
    {
        const int ticks = 80;

        EntitySpec DeployerSpec()
        {
            return new EntitySpec
            {
                Id = "replay.deployer",
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Deployer", "deploy.name", "deploy.role", "DPL", IconGlyph.Turret),
                Stats = new StatsSpec(UnitWeightClass.Medium, ArmorTag.Vehicle, MaxHp: 120, SightRange: 420, Cost: 80, TechTier: 1),
                Movement = new MovementSpec(MovementDomain.Land, Speed: 120, TurnRate: 6),
                Weapons =
                [
                    WeaponMountSpec.Omni("main", WeaponKind.NeedleRifle, Vector2.Zero, fireWhileMoving: false),
                ],
                Abilities =
                [
                    new AbilitySpec(AbilityKind.Deploy, Radius: 0.45f, Value: 1.6f),
                ],
            };
        }

        EntitySpec TargetSpec()
        {
            return new EntitySpec
            {
                Id = "replay.deploy_target",
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Deploy Target", "deploy.target.name", "deploy.target.role", "TGT", IconGlyph.Infantry),
                Stats = new StatsSpec(UnitWeightClass.Medium, ArmorTag.Vehicle, MaxHp: 200, SightRange: 100, Cost: 50, TechTier: 1),
                Movement = new MovementSpec(MovementDomain.Land, Speed: 0, TurnRate: 0),
            };
        }

        EntityWorld BuildDeployWorld()
        {
            var world = new EntityWorld(seed: 9494);
            world.AddSystem(new AbilitySystem());
            world.AddSystem(new CombatSystem());
            world.AddSystem(new ProjectileSystem());
            world.AddSystem(new MovementSystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);

            world.Spawn(DeployerSpec(), new OwnerId(1), EntityTransform.At(Vector2.Zero), new EntityComponentState[]
            {
                new HealthComponentState(120, 120),
                new MovementComponentState(Vector2.Zero, new Vector2(80, 0)),
                new VisionComponentState(420),
                new StanceComponentState(UnitStance.Hold),
                new WeaponUserComponentState(new[]
                {
                    new WeaponMountRuntimeState("main", WeaponKind.NeedleRifle, 0, 0),
                }, new EntityId(2), CombatTargetKind.Unit, AttackTargetIsManual: true),
                new AbilityRuntimeComponentState(new[]
                {
                    new AbilityCooldownState(AbilityKind.Deploy, 0),
                }),
            });
            world.Spawn(TargetSpec(), new OwnerId(2), EntityTransform.At(new Vector2(260, 0)), new EntityComponentState[]
            {
                new HealthComponentState(200, 200),
            });

            return world;
        }

        var deployCommands = new List<EntityCommand>
        {
            new AbilityEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 1, AbilityKind.Deploy),
            new AbilityEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 45, AbilityKind.Deploy),
        };
        AssertDeterministic("deploy", BuildDeployWorld, deployCommands, ticks, 10);

        var world = BuildDeployWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        foreach (var command in deployCommands)
        {
            buffer.Enqueue(command);
        }

        var shotsBeforeSetup = 0;
        var shotsAfterSetupBeforeUndeploy = 0;
        var shotsAfterUndeploy = 0;
        for (var tick = 1; tick <= ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
            var shots = world.Events.Drain().Count(evt => evt is WeaponFiredEvent);
            if (tick <= 14)
            {
                shotsBeforeSetup += shots;
            }
            else if (tick < 45)
            {
                shotsAfterSetupBeforeUndeploy += shots;
            }
            else if (tick > 45)
            {
                shotsAfterUndeploy += shots;
            }
        }

        var deployer = world.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var deploy = deployer.Components.Require<DeployComponentState>();
        var movement = deployer.Components.Require<MovementComponentState>();
        var targetHp = world.OrderedEntities.Single(entity => entity.Id.Value == 2).Components.Require<HealthComponentState>().Hp;

        Assert(shotsBeforeSetup == 0, $"deploy setup should block firing, got {shotsBeforeSetup} early shots");
        Assert(shotsAfterSetupBeforeUndeploy > 0, "deployed range multiplier should allow firing at a target outside base range");
        Assert(!deploy.IsDeployed && deploy.RangeMultiplier == 1, "second Deploy command should undeploy the unit");
        Assert(shotsAfterUndeploy == 0, $"undeployed unit should stop firing at out-of-base-range target, got {shotsAfterUndeploy} shots");
        Assert(movement.MoveTarget is null && movement.Velocity == Vector2.Zero, "deploy should stop and hold movement");
        Assert(targetHp < 200, "deployed shots should damage the target");
        Console.WriteLine($"OK [deploy]: setup shots {shotsBeforeSetup}, deployed shots {shotsAfterSetupBeforeUndeploy}, undeployed shots {shotsAfterUndeploy}, target hp {targetHp:0.0}.");
    }

    static void AssertLiveAbilityPlayerCommandBridge()
    {
        var battlefield = new UnitBattlefield();
        var scout = battlefield.Spawn<CatSpecial>(PlayerSlotId.One, Vector2.Zero);
        battlefield.SelectUnitsByIds(PlayerSlotId.One, [scout.Id]);

        var payload = PlayerCommandPayload.ForAbilityPoint(
            battlefield.SelectedUnitEntityIds(PlayerSlotId.One),
            AbilityKind.Scan,
            120,
            0);
        var result = battlefield.SubmitLiveLocalPlayerCommand(PlayerSlotId.One, PlayerCommandKind.Ability, payload);
        var caster = battlefield.UnitEntityByInstanceId(scout.Id) ?? throw new InvalidOperationException("Live ability caster entity missing.");
        var cooldown = caster.Components.Require<AbilityRuntimeComponentState>()
            .Cooldowns.Single(state => state.Kind == AbilityKind.Scan)
            .CooldownRemaining;
        var scanReveals = battlefield.EntityWorld.OrderedEntities.Count(entity => entity.Components.Has<ScanRevealComponentState>());

        battlefield.Update(0.5);
        var tickedCooldown = caster.Components.Require<AbilityRuntimeComponentState>()
            .Cooldowns.Single(state => state.Kind == AbilityKind.Scan)
            .CooldownRemaining;

        Assert(result.AcceptedCount == 1, $"live PlayerCommandKind.Ability should be accepted once, got {result.AcceptedCount}");
        Assert(scanReveals == 1, $"live Scan ability should spawn one reveal entity, got {scanReveals}");
        Assert(cooldown > 0, $"live Scan ability should set cooldown, got {cooldown:0.000}");
        Assert(tickedCooldown < cooldown, $"live ability cooldown should tick in UnitBattlefield.Update, got {tickedCooldown:0.000} >= {cooldown:0.000}");
        Console.WriteLine($"OK [live-ability-command]: accepted {result.AcceptedCount}, scan reveals {scanReveals}, cooldown {cooldown:0.00}->{tickedCooldown:0.00}.");
    }

    static void AssertAbilityCostAndTargetLegality()
    {
        const int ticks = 45;

        EntitySpec CasterSpec()
        {
            return new EntitySpec
            {
                Id = "replay.ability_legality_caster",
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Ability Caster", "ability.legality.name", "ability.legality.role", "ABL", IconGlyph.Settings),
                Abilities =
                [
                    new AbilitySpec(
                        AbilityKind.RepairField,
                        Radius: 90,
                        Value: 10,
                        Cost: 25,
                        TargetRule: AbilityTargetRule.FriendlyPointOrEntity),
                ],
            };
        }

        EntitySpec TargetSpec(string id)
        {
            return new EntitySpec
            {
                Id = id,
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Ability Target", "ability.target.name", "ability.target.role", "TGT", IconGlyph.Infantry),
            };
        }

        EntityWorld BuildAbilityLegalityWorld()
        {
            var world = new EntityWorld(seed: 9595);
            world.AddSystem(new AbilitySystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);
            world.ResourceInventory(new OwnerId(1)).Credits = 30;

            world.Spawn(CasterSpec(), new OwnerId(1), EntityTransform.At(Vector2.Zero), new EntityComponentState[]
            {
                new HealthComponentState(100, 100),
                new AbilityRuntimeComponentState(new[]
                {
                    new AbilityCooldownState(AbilityKind.RepairField, 0),
                }),
            });
            world.Spawn(TargetSpec("replay.ability_legality_ally"), new OwnerId(1), EntityTransform.At(new Vector2(20, 0)), new EntityComponentState[]
            {
                new HealthComponentState(50, 100),
            });
            world.Spawn(TargetSpec("replay.ability_legality_enemy"), new OwnerId(2), EntityTransform.At(new Vector2(25, 0)), new EntityComponentState[]
            {
                new HealthComponentState(40, 100),
            });

            return world;
        }

        var abilityCommands = new List<EntityCommand>
        {
            new AbilityEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 1, AbilityKind.RepairField, new EntityId(3)),
            new AbilityEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 2, AbilityKind.RepairField, new EntityId(2)),
            new AbilityEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 3, AbilityKind.RepairField, new EntityId(2)),
            new AbilityEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 40, AbilityKind.RepairField, new EntityId(2)),
        };
        AssertDeterministic("ability-legality", BuildAbilityLegalityWorld, abilityCommands, ticks, 9);

        var world = BuildAbilityLegalityWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        foreach (var command in abilityCommands)
        {
            buffer.Enqueue(command);
        }

        for (var tick = 1; tick <= ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
        }

        var caster = world.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var ally = world.OrderedEntities.Single(entity => entity.Id.Value == 2).Components.Require<HealthComponentState>();
        var enemy = world.OrderedEntities.Single(entity => entity.Id.Value == 3).Components.Require<HealthComponentState>();
        var credits = world.ResourceInventory(new OwnerId(1)).Credits;
        var cooldown = caster.Components.Require<AbilityRuntimeComponentState>()
            .Cooldowns.Single(state => state.Kind == AbilityKind.RepairField)
            .CooldownRemaining;

        Assert(Math.Abs(ally.Hp - 60) < 0.001f, $"one legal paid repair should heal ally to 60, got {ally.Hp}");
        Assert(Math.Abs(enemy.Hp - 40) < 0.001f, $"hostile target should be illegal for friendly repair, got enemy hp {enemy.Hp}");
        Assert(credits == 5, $"only one successful paid ability should spend credits 30 -> 5, got {credits}");
        Assert(Math.Abs(cooldown) < 0.001f, $"failed casts should not refresh cooldown, got {cooldown:0.000}");
        Console.WriteLine($"OK [ability-legality]: ally hp {ally.Hp}, enemy hp {enemy.Hp}, credits {credits}, cooldown {cooldown:0.00}s.");
    }

    static void AssertTargetedRepairCommand()
    {
        const int ticks = 60;

        EntitySpec RepairerSpec()
        {
            return new EntitySpec
            {
                Id = "replay.targeted_repairer",
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Targeted Repairer", "repairer.name", "repairer.role", "REP", IconGlyph.Settings),
                Movement = new MovementSpec(MovementDomain.Land, Speed: 60, TurnRate: 8),
                Collision = new CollisionSpec(Radius: 10, Mass: 1, PushPriority: 1),
                Abilities =
                [
                    new AbilitySpec(AbilityKind.RepairField, Radius: 40, Value: 30),
                ],
            };
        }

        EntitySpec TargetSpec(string id)
        {
            return new EntitySpec
            {
                Id = id,
                Kind = EntityKind.Unit,
                Display = new EntityDisplaySpec("Repairable Target", "repair.target.name", "repair.target.role", "TGT", IconGlyph.Infantry),
            };
        }

        EntityWorld BuildTargetedRepairWorld()
        {
            var world = new EntityWorld(seed: 9696);
            world.AddSystem(new CommandSystem());
            world.AddSystem(new RepairSystem());
            world.AddSystem(new MovementSystem());
            world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);
            world.ResourceInventory(new OwnerId(1)).Credits = 8;

            world.Spawn(RepairerSpec(), new OwnerId(1), EntityTransform.At(Vector2.Zero), new EntityComponentState[]
            {
                new HealthComponentState(80, 80),
                new CommandableComponentState(),
                new MovementComponentState(Vector2.Zero),
                new MovementProfileComponentState(MaxSpeed: 60, ArriveRadius: 4),
                new CollisionComponentState(10, 1, 1, true),
            });
            world.Spawn(TargetSpec("replay.targeted_repair_ally"), new OwnerId(1), EntityTransform.At(new Vector2(90, 0)), new EntityComponentState[]
            {
                new HealthComponentState(50, 100),
            });
            world.Spawn(TargetSpec("replay.targeted_repair_enemy"), new OwnerId(2), EntityTransform.At(new Vector2(20, 0)), new EntityComponentState[]
            {
                new HealthComponentState(40, 100),
            });

            return world;
        }

        var repairCommands = new List<EntityCommand>
        {
            new RepairEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 1, new EntityId(3)),
            new RepairEntityCommand(new OwnerId(1), new[] { new EntityId(1) }, 2, new EntityId(2)),
        };
        AssertDeterministic("targeted-repair", BuildTargetedRepairWorld, repairCommands, ticks, 10);

        var world = BuildTargetedRepairWorld();
        var clock = new SimClock();
        var buffer = new EntityCommandBuffer();
        foreach (var command in repairCommands)
        {
            buffer.Enqueue(command);
        }

        var movedTowardTarget = false;
        for (var tick = 1; tick <= ticks; tick++)
        {
            world.Step(tick, clock.FixedDelta, buffer.DrainUpToTick(tick));
            var repairer = world.OrderedEntities.Single(entity => entity.Id.Value == 1);
            if (tick is > 2 and < 25
                && repairer.Components.TryGet<MovementComponentState>(out var movement)
                && movement.MoveTarget is not null)
            {
                movedTowardTarget = true;
            }
        }

        var finalRepairer = world.OrderedEntities.Single(entity => entity.Id.Value == 1);
        var ally = world.OrderedEntities.Single(entity => entity.Id.Value == 2).Components.Require<HealthComponentState>();
        var enemy = world.OrderedEntities.Single(entity => entity.Id.Value == 3).Components.Require<HealthComponentState>();
        var credits = world.ResourceInventory(new OwnerId(1)).Credits;
        var finalMovement = finalRepairer.Components.Require<MovementComponentState>();

        Assert(movedTowardTarget, "repair command should move repairer toward an out-of-range friendly target");
        Assert(Math.Abs(ally.Hp - 58) < 0.001f, $"targeted repair should spend 8 credits for 8 hp, got ally hp {ally.Hp}");
        Assert(Math.Abs(enemy.Hp - 40) < 0.001f, $"hostile repair target should be rejected, got enemy hp {enemy.Hp}");
        Assert(credits == 0, $"targeted repair should spend available credits, got {credits}");
        Assert(finalRepairer.Components.Has<RepairOrderComponentState>(), "repair order should remain queued when target is still damaged but credits are exhausted");
        Assert(finalMovement.MoveTarget is null, "repairer should stop once in repair range");
        Console.WriteLine($"OK [targeted-repair]: ally hp {ally.Hp}, enemy hp {enemy.Hp}, credits {credits}, moved {movedTowardTarget}.");
    }
}
