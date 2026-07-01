namespace ProceduralRts.Core;

/// <summary>
/// Turns repaired/lit signal nodes into ordinary ECS capabilities. The node's
/// authored signal data stays stable; this system emits or removes build-radius,
/// vision, and safe resource-regeneration aura components according to power and
/// atmosphere so other systems can stay generic.
/// </summary>
public sealed class SignalNetworkSystem : ISimSystem
{
    public void Step(SimContext context)
    {
        foreach (var entity in context.World.OrderedEntities)
        {
            if (!entity.Components.TryGet<SignalNetworkComponentState>(out var signal))
            {
                continue;
            }

            if (!IsActiveSignalNode(entity))
            {
                ClearSignalOutputs(entity);
                continue;
            }

            ApplySignalOutputs(entity, signal, context.World.ResourceAtmosphere);
        }
    }

    private static void ApplySignalOutputs(
        EntityInstance entity,
        SignalNetworkComponentState signal,
        ResourceAtmosphere atmosphere)
    {
        var dayControl = IsDayControlAtmosphere(atmosphere);
        var radius = dayControl ? signal.DayControlRadius : signal.NightVisionRadius;
        if (dayControl && signal.DayControlRadius > 0)
        {
            entity.Components.Set(new BuildRadiusComponentState(signal.DayControlRadius));
        }
        else
        {
            entity.Components.Remove<BuildRadiusComponentState>();
        }

        if (!dayControl && signal.NightVisionRadius > 0)
        {
            entity.Components.Set(new VisionComponentState(signal.NightVisionRadius));
        }
        else
        {
            entity.Components.Remove<VisionComponentState>();
        }

        if (radius > 0 && signal.SafetyAuraMultiplier > 0)
        {
            entity.Components.Set(new ResourceRegenerationAuraComponentState(
                radius,
                signal.SafetyAuraMultiplier,
                RequiresPowered: true));
        }
        else
        {
            entity.Components.Remove<ResourceRegenerationAuraComponentState>();
        }
    }

    private static bool IsDayControlAtmosphere(ResourceAtmosphere atmosphere)
    {
        return atmosphere is ResourceAtmosphere.Day or ResourceAtmosphere.Fog;
    }

    private static bool IsActiveSignalNode(EntityInstance entity)
    {
        if (entity.Components.TryGet<HealthComponentState>(out var health) && health.Hp <= 0)
        {
            return false;
        }

        if (entity.Components.TryGet<ConstructionComponentState>(out var construction) && construction.Progress < 1)
        {
            return false;
        }

        return entity.Components.TryGet<PowerComponentState>(out var power) && power.Powered;
    }

    private static void ClearSignalOutputs(EntityInstance entity)
    {
        entity.Components.Remove<BuildRadiusComponentState>();
        entity.Components.Remove<VisionComponentState>();
        entity.Components.Remove<ResourceRegenerationAuraComponentState>();
    }
}
