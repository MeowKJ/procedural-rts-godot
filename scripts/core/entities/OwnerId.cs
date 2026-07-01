namespace ProceduralRts.Core;

public readonly record struct OwnerId(int Value)
{
    public static readonly OwnerId None = new(0);

    public bool IsValid => Value > 0;

    public static OwnerId FromPlayerSlot(PlayerSlotId playerSlotId)
    {
        return new OwnerId(playerSlotId.Value);
    }

    public PlayerSlotId ToPlayerSlot()
    {
        return new PlayerSlotId(Value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
