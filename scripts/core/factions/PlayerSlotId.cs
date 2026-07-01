namespace ProceduralRts.Core;

public readonly record struct PlayerSlotId(int Value)
{
    public static readonly PlayerSlotId One = new(1);
    public static readonly PlayerSlotId Two = new(2);
    public static readonly PlayerSlotId Three = new(3);
    public static readonly PlayerSlotId Four = new(4);
}
