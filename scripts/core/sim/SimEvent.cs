using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// Things the simulation reports for presentation to react to (effects, audio,
/// alerts, HUD pulses). Events are pure data: the View drains them after each
/// tick and renders, but never writes back. Emitting an event must not change
/// simulation state, so the command log alone reproduces the run.
/// </summary>
public abstract record SimEvent(int Tick);

public sealed record EntityDamagedEvent(
    int Tick,
    EntityId Target,
    EntityId Attacker,
    float Damage,
    Vector2 Position) : SimEvent(Tick);

public sealed record EntityDestroyedEvent(
    int Tick,
    EntityId Entity,
    OwnerId Owner,
    Vector2 Position) : SimEvent(Tick);

public sealed record WeaponFiredEvent(
    int Tick,
    EntityId Source,
    string MountId,
    string WeaponId,
    Vector2 Muzzle,
    Vector2 TargetPosition,
    WeaponKind? WeaponKindAlias = null) : SimEvent(Tick)
{
    public WeaponFiredEvent(
        int Tick,
        EntityId Source,
        string MountId,
        WeaponKind Weapon,
        Vector2 Muzzle,
        Vector2 TargetPosition)
        : this(Tick, Source, MountId, WeaponCatalog.IdFor(Weapon), Muzzle, TargetPosition, Weapon)
    {
    }

    public WeaponKind Weapon => WeaponKindAlias
        ?? WeaponCatalog.KindForWeaponId(WeaponId)
        ?? throw new InvalidOperationException($"Weapon fired event has no WeaponKind alias for '{WeaponId}'.");
}

public sealed record ProjectileImpactEvent(
    int Tick,
    EntityId Projectile,
    EntityId Source,
    string AmmoId,
    Vector2 Position,
    bool HitPrimary) : SimEvent(Tick);

public sealed record CommandAcknowledgedEvent(
    int Tick,
    OwnerId Owner,
    CommandAcknowledgementKind Kind,
    Vector2 Position,
    CommandAcknowledgementAudioCue AudioCue) : SimEvent(Tick);

public sealed record ConstructionRejectedEvent(
    int Tick,
    OwnerId Owner,
    string BuildingSpecId,
    Vector2 Position,
    string Reason) : SimEvent(Tick);

public sealed record ConstructionCancelledEvent(
    int Tick,
    EntityId Entity,
    OwnerId Owner,
    string BuildingSpecId,
    Vector2 Position,
    int Refund,
    float Progress) : SimEvent(Tick);

public sealed record ConstructionDestroyedEvent(
    int Tick,
    EntityId Entity,
    OwnerId Owner,
    string BuildingSpecId,
    Vector2 Position,
    float Progress,
    ConstructionPhase Phase) : SimEvent(Tick);

/// <summary>
/// Append-only per-tick event collector. Systems push; the driver drains in
/// stable insertion order once the tick is complete.
/// </summary>
public sealed class SimEventSink
{
    private readonly List<SimEvent> _events = [];

    public void Raise(SimEvent simEvent)
    {
        _events.Add(simEvent);
    }

    public IReadOnlyList<SimEvent> Drain()
    {
        if (_events.Count == 0)
        {
            return Array.Empty<SimEvent>();
        }

        var snapshot = _events.ToArray();
        _events.Clear();
        return snapshot;
    }

    public void DrainInto(List<SimEvent> destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        destination.Clear();
        if (_events.Count == 0)
        {
            return;
        }

        destination.AddRange(_events);
        _events.Clear();
    }
}
