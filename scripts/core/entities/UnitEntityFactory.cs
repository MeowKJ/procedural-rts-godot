using Godot;

namespace ProceduralRts.Core;

public static class UnitEntityFactory
{
    private const int DefaultHarvesterCargoCapacity = 700;

    public static EntitySpec ToEntitySpec(this UnitSpec unitSpec)
    {
        var tags = CreateTags(unitSpec);

        return new EntitySpec
        {
            Id = unitSpec.Id,
            Kind = EntityKind.Unit,
            Display = new EntityDisplaySpec(
                unitSpec.Label,
                unitSpec.NameKey,
                unitSpec.RoleKey,
                unitSpec.ShortCode,
                unitSpec.Icon),
            Tags = tags,
            Stats = unitSpec.Stats,
            Movement = unitSpec.Movement,
            Collision = unitSpec.Collision,
            Weapons = unitSpec.Weapons,
            Abilities = unitSpec.Abilities,
            Production = unitSpec.Production,
            UnitArt = unitSpec.Art,
            Authoring = new EntityAuthoringMetadata(
                UnitFaction: unitSpec.Faction,
                TechTier: unitSpec.Stats.TechTier,
                RosterTags: tags),
        };
    }

    private static HashSet<string> CreateTags(UnitSpec unitSpec)
    {
        var tags = new HashSet<string>();
        foreach (var tag in unitSpec.RoleTags)
        {
            tags.Add(tag.ToString());
        }

        tags.Add(unitSpec.Archetype.ToString());
        return tags;
    }

    public static EntityInstance SpawnUnit(
        this EntityWorld world,
        UnitSpec unitSpec,
        OwnerId ownerId,
        Vector2 position,
        float facing = 0)
    {
        return world.Spawn(
            unitSpec.ToEntitySpec(),
            ownerId,
            EntityTransform.At(position, facing),
            InitialUnitComponents(world, unitSpec, facing, position));
    }

    private static IEnumerable<EntityComponentState> InitialUnitComponents(EntityWorld world, UnitSpec unitSpec, float facing, Vector2 position)
    {
        yield return new HealthComponentState(unitSpec.Stats.MaxHp, unitSpec.Stats.MaxHp);
        yield return new SelectableComponentState();
        yield return new CommandableComponentState();
        yield return new MovementComponentState(Vector2.Zero);
        // MovementProfile + Stance make the entity fully driven by the generic
        // MovementSystem/CombatSystem — no per-unit code. A new unit is just a spec.
        yield return new MovementProfileComponentState(
            MaxSpeed: unitSpec.Movement.Speed,
            ArriveRadius: MathF.Max(2f, unitSpec.Collision.Radius * 0.5f),
            TurnRate: unitSpec.Movement.TurnRate,
            TurnMode: unitSpec.Movement.TurnMode);
        yield return new CollisionComponentState(
            unitSpec.Collision.Radius,
            unitSpec.Collision.Mass,
            unitSpec.Collision.PushPriority,
            unitSpec.Collision.BlocksMovement);
        yield return new VisionComponentState(unitSpec.Stats.SightRange);
        yield return new WeaponUserComponentState(CreateWeaponMountStates(unitSpec, facing));
        if (unitSpec.Weapons.Count > 0)
        {
            yield return new VeterancyComponentState();
            yield return new StanceComponentState(UnitStance.Aggressive);
            yield return new AutonomyComponentState(
                AcquireRange: unitSpec.Stats.SightRange,
                LeashRange: MathF.Max(unitSpec.Stats.SightRange, WeaponRange(world, unitSpec) + unitSpec.Stats.SightRange * 0.25f),
                AnchorPosition: position);
        }

        yield return new PresentationPulseComponentState();

        if (unitSpec.HasAbility(AbilityKind.Harvest))
        {
            yield return new HarvesterComponentState();
            yield return new ResourceCargoComponentState(0, DefaultHarvesterCargoCapacity);
        }

        if (unitSpec.TryGetAbility(AbilityKind.Build, out var build) && build.Radius > 0)
        {
            yield return new BuildRadiusComponentState(build.Radius);
        }

        var activeAbilityCount = ActiveAbilityCount(unitSpec);
        if (activeAbilityCount > 0)
        {
            var activeAbilities = new AbilityCooldownState[activeAbilityCount];
            var activeAbilityIndex = 0;
            foreach (var ability in unitSpec.Abilities)
            {
                if (IsRuntimeActiveAbility(ability))
                {
                    activeAbilities[activeAbilityIndex++] = new AbilityCooldownState(ability.Kind, 0);
                }
            }

            yield return new AbilityRuntimeComponentState(activeAbilities);
        }
    }

    private static int ActiveAbilityCount(UnitSpec unitSpec)
    {
        var count = 0;
        foreach (var ability in unitSpec.Abilities)
        {
            if (IsRuntimeActiveAbility(ability))
            {
                count++;
            }
        }

        return count;
    }

    private static WeaponMountRuntimeState[] CreateWeaponMountStates(UnitSpec unitSpec, float facing)
    {
        var states = new WeaponMountRuntimeState[unitSpec.Weapons.Count];
        for (var index = 0; index < unitSpec.Weapons.Count; index++)
        {
            var mount = unitSpec.Weapons[index];
            states[index] = new WeaponMountRuntimeState(mount.MountId, mount.WeaponId, facing, 0);
        }

        return states;
    }

    private static bool IsRuntimeActiveAbility(AbilitySpec ability)
    {
        return ability.Kind is not AbilityKind.Harvest and not AbilityKind.Build;
    }

    private static float WeaponRange(EntityWorld world, UnitSpec unitSpec)
    {
        var range = 0f;
        foreach (var mount in unitSpec.Weapons)
        {
            if (world.TryGetWeaponDefinition(mount.WeaponId, out var weapon))
            {
                range = MathF.Max(range, weapon.Range);
            }
        }

        return range;
    }
}
