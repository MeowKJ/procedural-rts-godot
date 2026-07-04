namespace ProceduralRts.Core;

public static class ElementReactionResolver
{
    public static IReadOnlyList<ElementStatusInstance> ApplyPrimer(
        IReadOnlyList<ElementStatusInstance>? activeStatuses,
        ElementStatusDefinition primer)
    {
        ArgumentNullException.ThrowIfNull(primer);
        var definition = ElementStatusCatalog.For(primer.Id);

        var statuses = NormalizeActive(activeStatuses);
        var existingIndex = statuses.FindIndex(status => status.StatusId == definition.Id);
        if (existingIndex < 0)
        {
            statuses.Add(ElementStatusInstance.FromDefinition(definition));
            return Sort(statuses);
        }

        var existing = statuses[existingIndex];
        statuses[existingIndex] = definition.StackingMode switch
        {
            ElementStatusStackingMode.RefreshDuration => existing with
            {
                RemainingDuration = MathF.Max(existing.RemainingDuration, definition.DurationSeconds),
            },
            ElementStatusStackingMode.StackAndRefresh => existing with
            {
                RemainingDuration = definition.DurationSeconds,
                Stacks = Math.Min(definition.MaxStacks, existing.Stacks + 1),
            },
            ElementStatusStackingMode.IgnoreWhileActive => existing,
            _ => throw new ArgumentOutOfRangeException(nameof(primer), "Unknown element status stacking mode."),
        };

        return Sort(statuses);
    }

    public static IReadOnlyList<ElementStatusInstance> TickStatuses(
        IReadOnlyList<ElementStatusInstance>? activeStatuses,
        float deltaSeconds)
    {
        if (deltaSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Element status ticking cannot move backward.");
        }

        var next = new List<ElementStatusInstance>();
        foreach (var status in NormalizeActive(activeStatuses))
        {
            var remaining = status.RemainingDuration - deltaSeconds;
            if (remaining > 0)
            {
                next.Add(status with { RemainingDuration = remaining });
            }
        }

        return Sort(next);
    }

    public static ElementReactionResolution Resolve(
        IReadOnlyList<ElementStatusInstance>? activeStatuses,
        string triggerElementId)
    {
        _ = DamageElementCatalog.For(triggerElementId);
        var statuses = NormalizeActive(activeStatuses);
        foreach (var status in statuses)
        {
            var reaction = ElementReactionCatalog.Match(status.StatusId, triggerElementId);
            if (reaction is null)
            {
                continue;
            }

            var remaining = reaction.ConsumesPrimer
                ? ConsumePrimer(statuses, status)
                : Sort(statuses);
            return new ElementReactionResolution(true, reaction, status, remaining);
        }

        return new ElementReactionResolution(false, null, null, Sort(statuses));
    }

    private static List<ElementStatusInstance> NormalizeActive(IReadOnlyList<ElementStatusInstance>? activeStatuses)
    {
        var statuses = new List<ElementStatusInstance>();
        if (activeStatuses is null)
        {
            return statuses;
        }

        foreach (var status in activeStatuses)
        {
            var definition = ElementStatusCatalog.For(status.StatusId);
            if (status.SourceElementId != definition.SourceElementId)
            {
                throw new InvalidOperationException($"Element status '{status.StatusId}' has source '{status.SourceElementId}' but catalog expects '{definition.SourceElementId}'.");
            }

            if (status.Stacks > definition.MaxStacks)
            {
                throw new InvalidOperationException($"Element status '{status.StatusId}' has {status.Stacks} stacks but catalog max is {definition.MaxStacks}.");
            }

            if (status.RemainingDuration > 0)
            {
                statuses.Add(status);
            }
        }

        statuses.Sort(CompareStatus);
        return statuses;
    }

    private static IReadOnlyList<ElementStatusInstance> ConsumePrimer(
        IReadOnlyList<ElementStatusInstance> activeStatuses,
        ElementStatusInstance consumed)
    {
        var next = new List<ElementStatusInstance>();
        foreach (var status in activeStatuses)
        {
            if (status.StatusId != consumed.StatusId)
            {
                next.Add(status);
                continue;
            }

            if (status.Stacks > 1)
            {
                next.Add(status with { Stacks = status.Stacks - 1 });
            }
        }

        return Sort(next);
    }

    private static IReadOnlyList<ElementStatusInstance> Sort(List<ElementStatusInstance> statuses)
    {
        statuses.Sort(CompareStatus);
        return statuses.ToArray();
    }

    private static int CompareStatus(ElementStatusInstance left, ElementStatusInstance right)
    {
        var byId = string.Compare(left.StatusId, right.StatusId, StringComparison.Ordinal);
        return byId != 0 ? byId : string.Compare(left.SourceElementId, right.SourceElementId, StringComparison.Ordinal);
    }
}
