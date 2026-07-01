namespace ProceduralRts.Core;

/// <summary>
/// Marks an entity as mattering to win/lose conditions. An owner that has at
/// least one victory-critical entity is defeated when it loses all of them
/// (e.g. the HQ). Kept as data so OutcomeSystem reads it generically rather than
/// hard-coding "HQ building".
/// </summary>
public sealed record ObjectiveComponentState(
    bool IsVictoryCritical = false) : EntityComponentState;
