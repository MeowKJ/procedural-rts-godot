namespace ProceduralRts.Core;

public sealed class PlayerRelationTable
{
    private readonly Dictionary<(PlayerSlotId Viewer, PlayerSlotId Subject), PlayerRelation> _relations = [];

    public PlayerRelation Relation(PlayerSlotId viewer, PlayerSlotId subject)
    {
        if (viewer == subject)
        {
            return PlayerRelation.Self;
        }

        return _relations.TryGetValue((viewer, subject), out var relation)
            ? relation
            : PlayerRelation.Hostile;
    }

    public void Set(PlayerSlotId first, PlayerSlotId second, PlayerRelation relation)
    {
        if (first == second)
        {
            return;
        }

        _relations[(first, second)] = relation;
        _relations[(second, first)] = relation;
    }

    public bool CanAttack(PlayerSlotId attacker, PlayerSlotId target)
    {
        return Relation(attacker, target) == PlayerRelation.Hostile;
    }
}
