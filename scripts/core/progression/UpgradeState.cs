namespace ProceduralRts.Core;

public sealed class UpgradeState
{
    private readonly SortedSet<string> _completed = new(StringComparer.Ordinal);

    public CompletedUpgradeIds CompletedIds => new(_completed);

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

    public readonly struct CompletedUpgradeIds
    {
        private readonly SortedSet<string> _completed;

        internal CompletedUpgradeIds(SortedSet<string> completed)
        {
            _completed = completed;
        }

        public SortedSet<string>.Enumerator GetEnumerator()
        {
            return _completed.GetEnumerator();
        }
    }
}
