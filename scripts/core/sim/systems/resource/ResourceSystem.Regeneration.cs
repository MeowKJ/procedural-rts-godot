namespace ProceduralRts.Core;

public sealed partial class ResourceSystem
{
    private static void StepResourceRegeneration(EntityWorld world, float dt)
    {
        foreach (var entity in world.OrderedEntities)
        {
            if (!entity.Components.TryGet<ResourceNodeComponentState>(out var node))
            {
                continue;
            }

            var cap = world.EconomyTuning.RegenerationCapFor(node);
            if (node.Amount >= cap)
            {
                entity.Components.Set(node with { RegenerationProgress = 0 });
                continue;
            }

            var rate = world.EconomyTuning.RegenerationRateFor(
                node,
                world.ResourceAtmosphere,
                ResourceRegenerationAuraMultiplier(world, entity));
            if (rate <= 0)
            {
                continue;
            }

            var progress = node.RegenerationProgress + rate * dt;
            var whole = (int)MathF.Floor(progress);
            if (whole <= 0)
            {
                entity.Components.Set(node with { RegenerationProgress = progress });
                continue;
            }

            var nextAmount = Math.Min(cap, node.Amount + whole);
            entity.Components.Set(node with
            {
                Amount = nextAmount,
                RegenerationProgress = nextAmount >= cap ? 0 : progress - whole,
            });
        }
    }

    private static float ResourceRegenerationAuraMultiplier(EntityWorld world, EntityInstance resource)
    {
        var multiplier = 1f;
        foreach (var candidate in world.OrderedEntities)
        {
            if (!candidate.Components.TryGet<ResourceRegenerationAuraComponentState>(out var aura)
                || aura.Radius <= 0
                || aura.Multiplier <= 0)
            {
                continue;
            }

            if (aura.RequiresPowered
                && (!candidate.Components.TryGet<PowerComponentState>(out var power) || !power.Powered))
            {
                continue;
            }

            if (candidate.Transform.Position.DistanceSquaredTo(resource.Transform.Position) <= aura.Radius * aura.Radius)
            {
                multiplier = MathF.Max(multiplier, aura.Multiplier);
            }
        }

        return multiplier;
    }
}
