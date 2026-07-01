using Godot;

namespace ProceduralRts.Core;

public static class PresentationCatalog
{
    public static FactionPresentationDescriptor Faction(FactionId factionId)
    {
        var definition = FactionCatalog.For(factionId);
        return new FactionPresentationDescriptor(
            definition.Id,
            definition.DisplayNameKey,
            definition.ShortCode,
            definition.Glyph,
            definition.Accent,
            definition.HudColor,
            FactionVisualPolicy.FactionHudColor(definition.Id));
    }

    public static EntityPresentationDescriptor Unit(
        string designId,
        Owner owner,
        FactionId factionId,
        Owner viewerOwner,
        FactionId viewerFaction)
    {
        var spec = UnitDesignCatalog.Spec(designId);
        var unit = UnitPresentationCatalog.ForSpec(spec);
        var faction = Faction(factionId);
        var relation = FactionRelations.Relation(viewerOwner, viewerFaction, owner, factionId);
        return new EntityPresentationDescriptor(
            null,
            factionId,
            unit.NameKey,
            unit.RoleKey,
            unit.ShortCode,
            unit.Icon,
            unit.RoleGlyph == IconGlyph.None ? unit.Art.StatusGlyph : unit.RoleGlyph,
            faction.Glyph,
            unit.PortraitMode,
            unit.Accent,
            faction.Accent,
            FactionVisualPolicy.EntityAccent(viewerOwner, viewerFaction, owner, factionId, unit.Accent),
            FactionVisualPolicy.RelationOverlay(relation),
            FactionVisualPolicy.MinimapPip(viewerOwner, viewerFaction, owner, factionId));
    }

    public static EntityPresentationDescriptor Building(
        string kind,
        Owner owner,
        FactionId factionId,
        Owner viewerOwner,
        FactionId viewerFaction)
    {
        var spec = BuildSpecCatalog.For(kind);
        var faction = Faction(factionId);
        var relation = FactionRelations.Relation(viewerOwner, viewerFaction, owner, factionId);
        return new EntityPresentationDescriptor(
            kind,
            factionId,
            spec.NameKey,
            spec.RoleKey,
            spec.ShortCode,
            spec.Icon,
            spec.RoleGlyph,
            faction.Glyph,
            "building",
            spec.Accent,
            faction.Accent,
            FactionVisualPolicy.EntityAccent(viewerOwner, viewerFaction, owner, factionId, spec.Accent),
            FactionVisualPolicy.RelationOverlay(relation),
            FactionVisualPolicy.MinimapPip(viewerOwner, viewerFaction, owner, factionId));
    }
}
