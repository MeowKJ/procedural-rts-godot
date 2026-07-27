namespace ProceduralRts.Core;

public readonly record struct PlayerCommandPoint(float X, float Y)
{
    public bool IsFinite => float.IsFinite(X) && float.IsFinite(Y);
}

public readonly record struct PlayerCommandBuildFacing(int QuarterTurns)
{
    public const string InvalidPayloadMessage = "Build facing quarter-turn must be in the range 0..3.";

    public bool TryResolveCanonicalRadians(out float radians)
    {
        radians = 0;
        if (QuarterTurns is < 0 or > 3)
        {
            return false;
        }

        radians = QuarterTurns * (MathF.PI * 0.5f);
        return true;
    }
}

public readonly record struct PlayerCommandPayload(
    IReadOnlyList<EntityId>? Subjects,
    bool HasTargetPoint,
    PlayerCommandPoint TargetPoint,
    EntityId TargetEntity,
    CombatTargetKind TargetKind,
    string SpecId,
    AbilityKind Ability,
    UnitStance Stance,
    MoveCommandMode MoveMode,
    PlayerCommandBuildFacing BuildFacing = default)
{
    public static PlayerCommandPayload Empty { get; } = new(
        Array.Empty<EntityId>(),
        HasTargetPoint: false,
        default,
        default,
        CombatTargetKind.Unit,
        string.Empty,
        AbilityKind.Harvest,
        UnitStance.Hold,
        MoveCommandMode.Direct);

    public IReadOnlyList<EntityId> SubjectIds => Subjects ?? Array.Empty<EntityId>();

    public static PlayerCommandPayload ForSubjects(IReadOnlyList<EntityId> subjects)
    {
        return Empty with { Subjects = subjects };
    }

    public static PlayerCommandPayload ForPoint(
        IReadOnlyList<EntityId> subjects,
        float x,
        float y,
        MoveCommandMode moveMode = MoveCommandMode.Direct)
    {
        return Empty with
        {
            Subjects = subjects,
            HasTargetPoint = true,
            TargetPoint = new PlayerCommandPoint(x, y),
            MoveMode = moveMode,
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

    public static PlayerCommandPayload ForAbility(IReadOnlyList<EntityId> subjects, AbilityKind ability)
    {
        return Empty with
        {
            Subjects = subjects,
            Ability = ability,
        };
    }

    public static PlayerCommandPayload ForAbilityPoint(
        IReadOnlyList<EntityId> subjects,
        AbilityKind ability,
        float x,
        float y)
    {
        return Empty with
        {
            Subjects = subjects,
            Ability = ability,
            HasTargetPoint = true,
            TargetPoint = new PlayerCommandPoint(x, y),
        };
    }

    public static PlayerCommandPayload ForAbilityEntityTarget(
        IReadOnlyList<EntityId> subjects,
        AbilityKind ability,
        EntityId target)
    {
        return Empty with
        {
            Subjects = subjects,
            Ability = ability,
            TargetEntity = target,
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

    public static PlayerCommandPayload ForBuild(string specId, float x, float y, int quarterTurns)
    {
        return Empty with
        {
            SpecId = specId,
            HasTargetPoint = true,
            TargetPoint = new PlayerCommandPoint(x, y),
            BuildFacing = new PlayerCommandBuildFacing(quarterTurns),
        };
    }
}
