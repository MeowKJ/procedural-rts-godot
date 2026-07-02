namespace ProceduralRts.Core;

public readonly record struct PlayerCommandPoint(float X, float Y)
{
    public bool IsFinite => float.IsFinite(X) && float.IsFinite(Y);
}

public readonly record struct PlayerCommandPayload(
    IReadOnlyList<EntityId>? Subjects,
    bool HasTargetPoint,
    PlayerCommandPoint TargetPoint,
    EntityId TargetEntity,
    CombatTargetKind TargetKind,
    string SpecId,
    AbilityKind Ability,
    UnitStance Stance)
{
    public static PlayerCommandPayload Empty { get; } = new(
        Array.Empty<EntityId>(),
        HasTargetPoint: false,
        default,
        default,
        CombatTargetKind.Unit,
        string.Empty,
        AbilityKind.Harvest,
        UnitStance.Hold);

    public IReadOnlyList<EntityId> SubjectIds => Subjects ?? Array.Empty<EntityId>();

    public static PlayerCommandPayload ForSubjects(IReadOnlyList<EntityId> subjects)
    {
        return Empty with { Subjects = subjects };
    }

    public static PlayerCommandPayload ForPoint(IReadOnlyList<EntityId> subjects, float x, float y)
    {
        return Empty with
        {
            Subjects = subjects,
            HasTargetPoint = true,
            TargetPoint = new PlayerCommandPoint(x, y),
        };
    }

    public static PlayerCommandPayload ForEntityTarget(
        IReadOnlyList<EntityId> subjects,
        EntityId target,
        CombatTargetKind targetKind = CombatTargetKind.Unit)
    {
        return Empty with
        {
            Subjects = subjects,
            TargetEntity = target,
            TargetKind = targetKind,
        };
    }

    public static PlayerCommandPayload ForSpec(string specId, IReadOnlyList<EntityId>? subjects = null)
    {
        return Empty with
        {
            Subjects = subjects ?? Array.Empty<EntityId>(),
            SpecId = specId,
        };
    }
}
