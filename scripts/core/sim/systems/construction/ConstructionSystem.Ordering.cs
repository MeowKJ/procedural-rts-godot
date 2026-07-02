namespace ProceduralRts.Core;

public sealed partial class ConstructionSystem
{
    private static readonly Comparison<EntityId> EntityIdSortComparison = CompareEntityIds;

    private static void CollectRequiredBuildings(BuildSpec spec, List<string> result)
    {
        result.Clear();
        foreach (var required in spec.RequiredBuildings)
        {
            result.Add(required);
        }

        result.Sort(StringComparer.Ordinal);
    }

    private static void CollectOrderedSubjects(IReadOnlyList<EntityId> subjects, List<EntityId> result)
    {
        result.Clear();
        foreach (var subject in subjects)
        {
            result.Add(subject);
        }

        result.Sort(EntityIdSortComparison);
    }

    private static int CompareEntityIds(EntityId left, EntityId right)
    {
        return left.Value.CompareTo(right.Value);
    }
}
