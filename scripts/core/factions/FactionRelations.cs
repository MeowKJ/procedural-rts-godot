namespace ProceduralRts.Core;

public static class FactionRelations
{
    public static FactionRelation Relation(
        Owner viewerOwner,
        FactionId viewerFaction,
        Owner subjectOwner,
        FactionId subjectFaction)
    {
        if (viewerOwner == subjectOwner && viewerFaction == subjectFaction)
        {
            return FactionRelation.Self;
        }

        if (viewerOwner == subjectOwner)
        {
            return FactionRelation.Allied;
        }

        return FactionRelation.Hostile;
    }

    public static bool IsSelf(Owner viewerOwner, FactionId viewerFaction, Owner subjectOwner, FactionId subjectFaction)
    {
        return Relation(viewerOwner, viewerFaction, subjectOwner, subjectFaction) == FactionRelation.Self;
    }

    public static bool IsAllied(Owner viewerOwner, FactionId viewerFaction, Owner subjectOwner, FactionId subjectFaction)
    {
        return Relation(viewerOwner, viewerFaction, subjectOwner, subjectFaction) is FactionRelation.Self or FactionRelation.Allied;
    }

    public static bool IsHostile(Owner viewerOwner, FactionId viewerFaction, Owner subjectOwner, FactionId subjectFaction)
    {
        return Relation(viewerOwner, viewerFaction, subjectOwner, subjectFaction) == FactionRelation.Hostile;
    }

    public static bool IsVisibleHostile(
        Owner viewerOwner,
        FactionId viewerFaction,
        Owner subjectOwner,
        FactionId subjectFaction,
        bool subjectVisible)
    {
        return subjectVisible && IsHostile(viewerOwner, viewerFaction, subjectOwner, subjectFaction);
    }
}
