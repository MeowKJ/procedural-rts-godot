namespace ProceduralRts.Core;

public readonly record struct EntityId(int Value)
{
    public static readonly EntityId None = new(0);

    public bool IsValid => Value > 0;

    public override string ToString()
    {
        return Value.ToString();
    }
}
