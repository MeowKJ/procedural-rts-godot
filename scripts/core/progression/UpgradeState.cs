namespace ProceduralRts.Core;

public sealed class UpgradeState
{
    private readonly SortedSet<string> _completed = new(StringComparer.Ordinal);

    public IReadOnlyList<string> CompletedIds => _completed.ToArray();

    public bool Complete(string id)
    {
        if (!UpgradeCatalog.Definitions.ContainsKey(id))
        {
            throw new InvalidOperationException($"Upgrade '{id}' is not defined.");
        }

        return _completed.Add(id);
    }

    public bool Has(string id)
    {
        return _completed.Contains(id);
    }
}
