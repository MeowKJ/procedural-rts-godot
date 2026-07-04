using System.Text;
using Godot;

namespace ProceduralRts.Core;

public static partial class EntityStateHash
{
    private const ulong Offset = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public static ulong Begin()
    {
        return Offset;
    }

    public static ulong Add(ulong hash, int value)
    {
        return Add(hash, unchecked((uint)value));
    }

    public static ulong Add(ulong hash, uint value)
    {
        hash = Add(hash, (byte)(value & 0xff));
        hash = Add(hash, (byte)((value >> 8) & 0xff));
        hash = Add(hash, (byte)((value >> 16) & 0xff));
        return Add(hash, (byte)((value >> 24) & 0xff));
    }

    public static ulong Add(ulong hash, ulong value)
    {
        hash = Add(hash, (uint)(value & 0xffffffffUL));
        return Add(hash, (uint)((value >> 32) & 0xffffffffUL));
    }

    public static ulong Add(ulong hash, float value)
    {
        return Add(hash, BitConverter.SingleToUInt32Bits(value));
    }

    public static ulong Add(ulong hash, Vector2 value)
    {
        hash = Add(hash, value.X);
        return Add(hash, value.Y);
    }

    public static ulong Add(ulong hash, string value)
    {
        Span<byte> utf8 = stackalloc byte[4];
        for (var index = 0; index < value.Length;)
        {
            var charCount = char.IsHighSurrogate(value[index])
                && index + 1 < value.Length
                && char.IsLowSurrogate(value[index + 1])
                    ? 2
                    : 1;
            var written = Encoding.UTF8.GetBytes(value.AsSpan(index, charCount), utf8);
            for (var byteIndex = 0; byteIndex < written; byteIndex++)
            {
                hash = Add(hash, utf8[byteIndex]);
            }

            index += charCount;
        }

        return Add(hash, 0);
    }

    public static ulong AddNullableString(ulong hash, string? value)
    {
        hash = Add(hash, value is null ? 0 : 1);
        return value is null ? hash : Add(hash, value);
    }

    public static ulong Add(
        ulong hash,
        EntityComponentState state,
        List<AbilityCooldownState>? abilityCooldownOrder = null,
        List<WeaponMountRuntimeState>? weaponMountOrder = null,
        List<UnitProductionQueueItem>? productionQueueOrder = null,
        List<EntityCommand>? commandQueueOrder = null,
        List<EntityId>? commandSubjectOrder = null)
    {
        return state switch
        {
            HealthComponentState health => Add(Add(hash, health.Hp), health.MaxHp),
            SelectableComponentState selectable => Add(Add(hash, selectable.Selected ? 1 : 0), selectable.AlertPulse),
            CommandableComponentState commandable => AddCommandable(hash, commandable),
            MovementComponentState movement => AddMovement(hash, movement),
            MovementProfileComponentState movementProfile => AddMovementProfile(hash, movementProfile),
            PatrolOrderComponentState patrol => AddPatrolOrder(hash, patrol),
            GuardOrderComponentState guard => AddGuardOrder(hash, guard),
            PathfindingComponentState pathfinding => AddPathfinding(hash, pathfinding),
            CollisionComponentState collision => AddCollision(hash, collision),
            VisionComponentState vision => Add(hash, vision.SightRange),
            AutonomyComponentState autonomy => AddAutonomy(hash, autonomy),
            RetaliationComponentState retaliation => AddRetaliation(hash, retaliation),
            WeaponUserComponentState weaponUser => AddWeaponUser(hash, weaponUser, weaponMountOrder),
            ProjectileComponentState projectile => AddProjectile(hash, projectile),
            VeterancyComponentState veterancy => AddVeterancy(hash, veterancy),
            RegenerationComponentState regen => AddRegeneration(hash, regen),
            HarvesterComponentState harvester => AddHarvester(hash, harvester),
            ResourceCargoComponentState cargo => Add(Add(hash, cargo.Cargo), cargo.Capacity),
            ResourceNodeComponentState node => AddResourceNode(hash, node),
            ResourceRegenerationAuraComponentState aura => AddResourceRegenerationAura(hash, aura),
            SignalNetworkComponentState signal => AddSignalNetwork(hash, signal),
            ProductionQueueComponentState production => AddProduction(hash, production, productionQueueOrder),
            AbilityRuntimeComponentState ability => AddAbilityRuntime(hash, ability, abilityCooldownOrder),
            ShieldComponentState shield => Add(Add(hash, shield.AbsorbRemaining), shield.DurationRemaining),
            ScanRevealComponentState scan => Add(Add(hash, scan.Radius), scan.DurationRemaining),
            DeployComponentState deploy => Add(Add(Add(hash, deploy.IsDeployed ? 1 : 0), deploy.SetupRemaining), deploy.RangeMultiplier),
            RepairOrderComponentState repair => AddRepairOrder(hash, repair),
            CommandQueueComponentState commandQueue => AddCommandQueue(hash, commandQueue, commandQueueOrder, commandSubjectOrder),
            FootprintComponentState footprint => Add(Add(hash, footprint.Size), (int)footprint.PlacementDomain),
            BuildingIdentityComponentState buildingIdentity => AddBuildingIdentity(hash, buildingIdentity),
            ConstructionIdentityComponentState constructionIdentity => Add(hash, constructionIdentity.Kind),
            ConstructionComponentState construction => AddConstruction(hash, construction),
            PowerComponentState power => Add(Add(Add(hash, power.Provided), power.Used), power.Powered ? 1 : 0),
            RallyPointComponentState rally => AddRallyPoint(hash, rally),
            DockComponentState dock => Add(Add(hash, dock.ReservedByEntityId ?? 0), dock.DockedEntityId ?? 0),
            BuildRadiusComponentState buildRadius => Add(hash, buildRadius.Radius),
            PresentationPulseComponentState pulse => Add(Add(Add(hash, pulse.CommandPulse), pulse.AlertPulse), pulse.HitPulse),
            _ => Add(hash, state.ToString() ?? string.Empty),
        };
    }

    private static ulong Add(ulong hash, byte value)
    {
        hash ^= value;
        return hash * Prime;
    }

    private static ulong AddNullableVector(ulong hash, Vector2? value)
    {
        hash = Add(hash, value.HasValue ? 1 : 0);
        return value.HasValue ? Add(hash, value.Value) : hash;
    }

    private static ulong AddCommandable(ulong hash, CommandableComponentState state)
    {
        hash = AddNullableVector(hash, state.PlayerIntentTarget);
        hash = AddNullableVector(hash, state.CommandVisualTarget);
        return Add(hash, (int)state.MoveMode);
    }

    private static ulong AddRallyPoint(ulong hash, RallyPointComponentState state)
    {
        hash = AddNullableVector(hash, state.Target);
        return Add(hash, state.TargetEntityId ?? 0);
    }

    private static ulong AddMovement(ulong hash, MovementComponentState state)
    {
        hash = Add(hash, state.Velocity);
        hash = AddNullableVector(hash, state.MoveTarget);
        hash = AddNullableVector(hash, state.FormationSlot);
        return Add(hash, state.FireAnchorRemaining);
    }

    private static ulong AddMovementProfile(ulong hash, MovementProfileComponentState state)
    {
        hash = Add(hash, state.MaxSpeed);
        hash = Add(hash, state.ArriveRadius);
        hash = Add(hash, state.TurnRate);
        return Add(hash, (int)state.TurnMode);
    }

    private static ulong AddPatrolOrder(ulong hash, PatrolOrderComponentState state)
    {
        hash = Add(hash, state.PointA);
        hash = Add(hash, state.PointB);
        return Add(hash, state.MovingToB ? 1 : 0);
    }

    private static ulong AddGuardOrder(ulong hash, GuardOrderComponentState state)
    {
        hash = Add(hash, state.TargetEntity.Value);
        hash = Add(hash, state.GuardPoint);
        return Add(hash, state.Radius);
    }

    private static ulong AddPathfinding(ulong hash, PathfindingComponentState state)
    {
        hash = Add(hash, state.Goal.X);
        hash = Add(hash, state.Goal.Y);
        hash = Add(hash, state.NextWaypointIndex);
        hash = Add(hash, state.Waypoints.Count);
        foreach (var waypoint in state.Waypoints)
        {
            hash = Add(hash, waypoint.X);
            hash = Add(hash, waypoint.Y);
        }

        return hash;
    }

    private static ulong AddCollision(ulong hash, CollisionComponentState state)
    {
        hash = Add(hash, state.Radius);
        hash = Add(hash, state.Mass);
        hash = Add(hash, state.PushPriority);
        return Add(hash, state.BlocksMovement ? 1 : 0);
    }

    private static ulong AddAutonomy(ulong hash, AutonomyComponentState state)
    {
        hash = Add(hash, state.AcquireRange);
        hash = Add(hash, state.LeashRange);
        return AddNullableVector(hash, state.AnchorPosition);
    }

    private static ulong AddRetaliation(ulong hash, RetaliationComponentState state)
    {
        hash = Add(hash, state.Target.Value);
        return Add(hash, state.LastThreatTick);
    }

    private static ulong AddProjectile(ulong hash, ProjectileComponentState state)
    {
        hash = Add(hash, state.Source.Value);
        hash = Add(hash, state.Target.Value);
        hash = Add(hash, state.WeaponId);
        hash = Add(hash, state.AmmoId);
        hash = Add(hash, state.Damage);
        hash = Add(hash, state.Velocity);
        hash = Add(hash, state.Speed);
        hash = Add(hash, state.TrackingStrength);
        hash = Add(hash, state.HitRadius);
        hash = Add(hash, state.LifetimeRemaining);
        return Add(hash, state.Interceptable ? 1 : 0);
    }

    private static ulong AddVeterancy(ulong hash, VeterancyComponentState state)
    {
        hash = Add(hash, state.Kills);
        hash = Add(hash, state.Experience);
        return Add(hash, state.Rank);
    }

    private static ulong AddRegeneration(ulong hash, RegenerationComponentState state)
    {
        hash = Add(hash, state.HpPerSecond);
        return Add(hash, state.Progress);
    }

    private static ulong AddHarvester(ulong hash, HarvesterComponentState state)
    {
        hash = Add(hash, (int)state.Mode);
        hash = Add(hash, state.FieldId ?? 0);
        hash = Add(hash, state.RefineryId ?? 0);
        hash = Add(hash, state.HarvestPulse);
        return Add(hash, state.Retreating ? 1 : 0);
    }

    private static ulong AddResourceNode(ulong hash, ResourceNodeComponentState state)
    {
        hash = Add(hash, state.Amount);
        hash = Add(hash, state.MaxAmount);
        hash = Add(hash, state.GatherRateModifier);
        hash = Add(hash, (int)state.DepletionBehavior);
        hash = Add(hash, (int)state.VisibilityRule);
        hash = Add(hash, (int)state.CorruptionState);
        return Add(hash, state.RegenerationProgress);
    }

    private static ulong AddResourceRegenerationAura(ulong hash, ResourceRegenerationAuraComponentState state)
    {
        hash = Add(hash, state.Radius);
        hash = Add(hash, state.Multiplier);
        return Add(hash, state.RequiresPowered ? 1 : 0);
    }

    private static ulong AddSignalNetwork(ulong hash, SignalNetworkComponentState state)
    {
        hash = Add(hash, (int)state.Kind);
        hash = Add(hash, state.DayControlRadius);
        hash = Add(hash, state.NightVisionRadius);
        return Add(hash, state.SafetyAuraMultiplier);
    }

    private static ulong AddRepairOrder(ulong hash, RepairOrderComponentState state)
    {
        hash = Add(hash, state.TargetId);
        hash = Add(hash, state.Range);
        hash = Add(hash, state.RepairPerSecond);
        hash = Add(hash, state.CreditCostPerHp);
        return Add(hash, state.RepairProgress);
    }

    private static ulong AddBuildingIdentity(ulong hash, BuildingIdentityComponentState state)
    {
        hash = Add(hash, state.LegacyBuildingId);
        hash = Add(hash, state.Kind);
        hash = Add(hash, state.PlayerSlotId.Value);
        return Add(hash, (int)state.Faction);
    }

    private static ulong AddConstruction(ulong hash, ConstructionComponentState state)
    {
        hash = Add(hash, state.Progress);
        hash = Add(hash, state.BuildTime);
        hash = Add(hash, state.Cost);
        hash = Add(hash, state.RefundRatio);
        hash = Add(hash, (int)state.PauseReason);
        return Add(hash, (int)state.Phase);
    }
}
