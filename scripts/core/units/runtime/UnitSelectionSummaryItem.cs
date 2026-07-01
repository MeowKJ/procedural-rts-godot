namespace ProceduralRts.Core;

public readonly record struct UnitSelectionSummaryItem(
    string DesignId,
    PlayerSlotId PlayerSlotId,
    UnitFactionId Faction,
    IconGlyph Icon,
    string Label,
    string ShortCode,
    int Count);
