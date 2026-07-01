namespace ProceduralRts.Core;

/// <summary>
/// Runtime relation lookup keyed by <see cref="OwnerId"/>. This is the single
/// authority for "can A target B" in the entity simulation — faction identity
/// never participates in runtime hostility (see docs/EntityFrameworkArchitecture.md
/// "Owner / Faction / Relation"). Defaults to hostile between distinct owners so
/// an unconfigured pair is never accidentally friendly.
/// </summary>
public sealed class OwnerRelationTable
{
    private readonly Dictionary<(int Viewer, int Subject), PlayerRelation> _relations = [];

    public PlayerRelation Relation(OwnerId viewer, OwnerId subject)
    {
        if (viewer.Value == subject.Value)
        {
            return PlayerRelation.Self;
        }

        return _relations.TryGetValue((viewer.Value, subject.Value), out var relation)
            ? relation
            : PlayerRelation.Hostile;
    }

    public void Set(OwnerId first, OwnerId second, PlayerRelation relation)
    {
        if (first.Value == second.Value)
        {
            return;
        }

        _relations[(first.Value, second.Value)] = relation;
        _relations[(second.Value, first.Value)] = relation;
    }

    public bool CanAttack(OwnerId attacker, OwnerId target)
    {
        return Relation(attacker, target) == PlayerRelation.Hostile;
    }
}
