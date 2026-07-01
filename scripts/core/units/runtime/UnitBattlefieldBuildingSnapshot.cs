using Godot;

namespace ProceduralRts.Core;

public readonly struct UnitBattlefieldBuildingSnapshot
{
    public UnitBattlefieldBuildingSnapshot(
        int id,
        string kind,
        PlayerSlotId playerSlotId,
        UnitFactionId faction,
        Vector2 position,
        float facing,
        float hp,
        Vector2 footprint)
    {
        Id = id;
        Kind = kind;
        PlayerSlotId = playerSlotId;
        Faction = faction;
        Position = position;
        Facing = facing;
        Hp = hp;
        Footprint = footprint;
    }

    public int Id { get; }
    public string Kind { get; }
    public PlayerSlotId PlayerSlotId { get; }
    public UnitFactionId Faction { get; }
    public Vector2 Position { get; }
    public float Facing { get; }
    public float Hp { get; }
    public Vector2 Footprint { get; }
}
