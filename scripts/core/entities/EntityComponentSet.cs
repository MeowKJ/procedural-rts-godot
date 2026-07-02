namespace ProceduralRts.Core;

public sealed class EntityComponentSet
{
    private readonly Dictionary<Type, EntityComponentState> _states = [];

    public IReadOnlyCollection<EntityComponentState> Values => _states.Values;

    public IReadOnlyList<EntityComponentState> StableValues => _states
        .OrderBy(pair => pair.Key.FullName, StringComparer.Ordinal)
        .Select(pair => pair.Value)
        .ToList();

    public void StableValuesInto(List<EntityComponentState> result)
    {
        result.Clear();
        foreach (var state in _states.Values)
        {
            result.Add(state);
        }

        result.Sort(CompareComponentTypes);
    }

    public void Set(EntityComponentState state)
    {
        _states[state.GetType()] = state;
    }

    public void Set<TState>(TState state)
        where TState : EntityComponentState
    {
        _states[typeof(TState)] = state;
    }

    public bool Has<TState>()
        where TState : EntityComponentState
    {
        return _states.ContainsKey(typeof(TState));
    }

    public bool TryGet<TState>(out TState state)
        where TState : EntityComponentState
    {
        if (_states.TryGetValue(typeof(TState), out var value) && value is TState typed)
        {
            state = typed;
            return true;
        }

        state = null!;
        return false;
    }

    public TState Require<TState>()
        where TState : EntityComponentState
    {
        return TryGet<TState>(out var state)
            ? state
            : throw new InvalidOperationException($"Entity component '{typeof(TState).Name}' is missing.");
    }

    public bool Remove<TState>()
        where TState : EntityComponentState
    {
        return _states.Remove(typeof(TState));
    }

    public void Clear()
    {
        _states.Clear();
    }

    private static int CompareComponentTypes(EntityComponentState left, EntityComponentState right)
    {
        return string.Compare(left.GetType().FullName, right.GetType().FullName, StringComparison.Ordinal);
    }
}
