using Godot;

namespace ProceduralRts.Core;

public static class FactionVisualPolicy
{
    public static readonly Color SelfOverlay = new("#3f8068");
    public static readonly Color AlliedOverlay = new("#6f8f72");
    public static readonly Color NeutralOverlay = new("#b98232");
    public static readonly Color HostileOverlay = new("#a83255");

    public static Color FactionAccent(FactionId factionId)
    {
        return FactionCatalog.For(factionId).Accent;
    }

    public static Color FactionHudColor(FactionId factionId)
    {
        return FactionCatalog.For(factionId).HudColor;
    }

    public static Color RelationOverlay(FactionRelation relation)
    {
        return relation switch
        {
            FactionRelation.Self => SelfOverlay,
            FactionRelation.Allied => AlliedOverlay,
            FactionRelation.Neutral => NeutralOverlay,
            FactionRelation.Hostile => HostileOverlay,
            _ => NeutralOverlay,
        };
    }

    public static Color EntityAccent(Owner viewerOwner, FactionId viewerFaction, Owner subjectOwner, FactionId subjectFaction, Color roleAccent)
    {
        var factionAccent = FactionAccent(subjectFaction);
        return factionAccent.Lerp(roleAccent, 0.38f);
    }

    public static Color MinimapPip(Owner viewerOwner, FactionId viewerFaction, Owner subjectOwner, FactionId subjectFaction)
    {
        var relation = FactionRelations.Relation(viewerOwner, viewerFaction, subjectOwner, subjectFaction);
        var factionAccent = FactionHudColor(subjectFaction);
        return relation == FactionRelation.Hostile
            ? factionAccent.Lerp(HostileOverlay, 0.42f)
            : relation == FactionRelation.Neutral
                ? factionAccent.Lerp(NeutralOverlay, 0.35f)
                : factionAccent.Lerp(RelationOverlay(relation), relation == FactionRelation.Self ? 0.22f : 0.34f);
    }

    public static Color CommandAccent(Owner viewerOwner, FactionId viewerFaction, Owner subjectOwner, FactionId subjectFaction, Color roleAccent)
    {
        var relation = FactionRelations.Relation(viewerOwner, viewerFaction, subjectOwner, subjectFaction);
        return relation == FactionRelation.Hostile
            ? HostileOverlay
            : EntityAccent(viewerOwner, viewerFaction, subjectOwner, subjectFaction, roleAccent);
    }
}
