using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// Authoritative combat: per armed entity, resolve a legal hostile target
/// (manual focus or stance-driven auto-acquire), rotate weapon mounts toward it,
/// hold at weapon range, and fire on cooldown. Damage flows through the shared
/// <see cref="WeaponCatalog"/> ammo/armor profile with a small seeded variance so
/// the same command log reproduces the same outcome. Deaths are queued for
/// removal after the tick and reported as events.
///
/// Iterates in stable EntityId order; acquisition ties break on lowest EntityId.
/// </summary>
public sealed partial class CombatSystem : ISimSystem
{
    private const float DamageVariance = 0.05f; // +/-5% seeded jitter per shot.
    private const float TargetStickinessRangeMultiplier = 1.18f;
    private const float TargetStickinessMinSlack = 48f;
    private const float TargetSwitchPriorityMargin = 1.35f;
    private const float TargetSwitchDistanceFactor = 0.45f;
    private const float AutoReacquireCooldownSeconds = 0.20f;
    private const float LastKnownTargetMemorySeconds = 1.50f;
    private const float LastKnownShortRangeChaseThreshold = 160f;
    private const float ThreatTargetPriorityMultiplier = 4f;
    private const float SharedAllyThreatPriorityMultiplier = 3.25f;
    private const float SharedAllyThreatRadius = 330f;
    private const float HoldSharedThreatSlack = 72f;
    private const int ThreatTargetMaxLocalCandidates = 3;
    public const float FireAnchorSeconds = 0.26f;
    private readonly SpatialGrid<EntityInstance> _targetGrid = new(1f);

    public void Step(SimContext context)
    {
        var world = context.World;
        var dt = context.FixedDelta;
        BuildTargetGrid(world);

        foreach (var entity in world.OrderedEntities)
        {
            if (!entity.Components.TryGet<WeaponUserComponentState>(out var weapon))
            {
                continue;
            }

            if (IsDead(entity))
            {
                continue;
            }

            weapon = TickAutoReacquireCooldown(entity, weapon, dt);
            weapon = TickLastKnownTargetMemory(entity, weapon, dt);

            if (IsUnpowered(entity))
            {
                world.Metrics.ClearAttackTarget(entity.Id.Value);
                CoolMounts(entity, weapon, dt);
                continue;
            }

            if (IsDeploying(entity))
            {
                world.Metrics.ClearAttackTarget(entity.Id.Value);
                CoolMounts(entity, weapon, dt);
                continue;
            }

            if (entity.Components.TryGet<AttackGroundOrderComponentState>(out var attackGround))
            {
                world.Metrics.ClearAttackTarget(entity.Id.Value);
                if (!WeaponEngagementQueries.CanAnyMountAttackGround(world, weapon))
                {
                    entity.Components.Remove<AttackGroundOrderComponentState>();
                    CoolMounts(entity, weapon, dt);
                    continue;
                }

                EngageGround(context, entity, weapon, attackGround.Target, dt);
                continue;
            }

            var target = ResolveTarget(context, entity, weapon);
            if (target is null)
            {
                // No target: tick cooldowns down, leave movement to other systems.
                world.Metrics.ClearAttackTarget(entity.Id.Value);
                CoolMounts(entity, weapon, dt);
                continue;
            }

            world.Metrics.RecordAttackTarget(entity.Id.Value, target.Id.Value);
            EngageTarget(context, entity, weapon, target, dt);
        }
    }

    private static bool IsDead(EntityInstance entity)
    {
        return entity.Components.TryGet<HealthComponentState>(out var health) && health.Hp <= 0;
    }

    private static bool IsUnpowered(EntityInstance entity)
    {
        return entity.Components.TryGet<PowerComponentState>(out var power) && !power.Powered;
    }

    private static bool IsDeploying(EntityInstance entity)
    {
        return entity.Components.TryGet<DeployComponentState>(out var deploy)
            && deploy.IsDeployed
            && deploy.SetupRemaining > 0;
    }
}
