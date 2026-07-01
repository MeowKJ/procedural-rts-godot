using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// Read-only debug/quality metrics derived purely from the simulation event
/// stream (docs/RTS99Design.md "Debug 指标"). Systems stay stateless: the driver
/// feeds each tick's drained events here and the collector accumulates. Nothing
/// in this type can affect simulation state, so it never breaks determinism.
/// </summary>
public sealed class SimMetrics
{
    private readonly SortedDictionary<string, SimSystemTiming> _systemTimings = [];
    private readonly HashSet<int> _dockWaitingHarvesters = [];
    private readonly Dictionary<int, Vector2> _lastMoveDirectionByEntity = [];
    private readonly Dictionary<int, Vector2> _lastMoveTargetByEntity = [];
    private readonly Dictionary<int, int> _lastAttackTargetByEntity = [];

    public int ShotsFired { get; private set; }
    public int Kills { get; private set; }
    public float TotalDamage { get; private set; }
    public int DroppedBacklogEvents { get; private set; }
    public int DroppedBacklogTicks { get; private set; }
    public double DroppedBacklogSeconds { get; private set; }
    public double EconomyElapsedSeconds { get; private set; }
    public int CreditsBanked { get; private set; }
    public double HarvesterIdleSeconds { get; private set; }
    public double HarvesterActiveTripSeconds { get; private set; }
    public double DockWaitSeconds { get; private set; }
    public int RefineryCongestionEvents { get; private set; }
    public int ResourceTripCompletions { get; private set; }
    public double MovementTravelDistance { get; private set; }
    public double MovementDirectProgressDistance { get; private set; }
    public int MovementCornerCount { get; private set; }
    public double MovementStuckSeconds { get; private set; }
    public int MovementRepathCount { get; private set; }
    public int TargetSwitchCount { get; private set; }
    public int AnchorPushEvents { get; private set; }
    public int ArrivalSamples { get; private set; }
    public double ArrivalJitterDistance { get; private set; }
    public int CompactnessSamples { get; private set; }
    public double CompactnessRadiusTotal { get; private set; }
    public double MaxCompactnessRadius { get; private set; }

    public double CreditsPerMinute => EconomyElapsedSeconds <= 0
        ? 0
        : CreditsBanked / EconomyElapsedSeconds * 60.0;

    public double AverageResourceTripSeconds => ResourceTripCompletions <= 0
        ? 0
        : HarvesterActiveTripSeconds / ResourceTripCompletions;

    public double PathInflationRatio => MovementDirectProgressDistance <= 0
        ? 0
        : MovementTravelDistance / MovementDirectProgressDistance;

    public double AverageArrivalJitterDistance => ArrivalSamples <= 0
        ? 0
        : ArrivalJitterDistance / ArrivalSamples;

    public double AverageCompactnessRadius => CompactnessSamples <= 0
        ? 0
        : CompactnessRadiusTotal / CompactnessSamples;

    /// <summary>Tick of the first shot fired, or -1 if none yet.</summary>
    public int TimeToFirstShotTick { get; private set; } = -1;

    public IReadOnlyDictionary<string, SimSystemTiming> SystemTimings => _systemTimings;

    public void Consume(IReadOnlyList<SimEvent> events)
    {
        foreach (var simEvent in events)
        {
            switch (simEvent)
            {
                case WeaponFiredEvent fired:
                    ShotsFired++;
                    if (TimeToFirstShotTick < 0)
                    {
                        TimeToFirstShotTick = fired.Tick;
                    }

                    break;

                case EntityDamagedEvent damaged:
                    TotalDamage += damaged.Damage;
                    break;

                case EntityDestroyedEvent:
                    Kills++;
                    break;
            }
        }
    }

    public void RecordClockBacklogDrop(int droppedTicks, double droppedSeconds)
    {
        if (droppedTicks <= 0 && droppedSeconds <= 0)
        {
            return;
        }

        DroppedBacklogEvents++;
        DroppedBacklogTicks += Math.Max(0, droppedTicks);
        DroppedBacklogSeconds += Math.Max(0, droppedSeconds);
    }

    public void RecordSystemStep(string systemName, double elapsedMs)
    {
        if (!_systemTimings.TryGetValue(systemName, out var current))
        {
            _systemTimings[systemName] = new SimSystemTiming(1, elapsedMs, elapsedMs, elapsedMs);
            return;
        }

        _systemTimings[systemName] = new SimSystemTiming(
            current.Samples + 1,
            current.TotalMs + elapsedMs,
            elapsedMs,
            Math.Max(current.MaxMs, elapsedMs));
    }

    public void RecordEconomyTick(double fixedDelta)
    {
        EconomyElapsedSeconds += Math.Max(0, fixedDelta);
    }

    public void RecordCreditsBanked(int credits)
    {
        CreditsBanked += Math.Max(0, credits);
    }

    public void RecordHarvesterIdle(double fixedDelta)
    {
        HarvesterIdleSeconds += Math.Max(0, fixedDelta);
    }

    public void RecordHarvesterActiveTrip(double fixedDelta)
    {
        HarvesterActiveTripSeconds += Math.Max(0, fixedDelta);
    }

    public void RecordDockWait(int harvesterId, double fixedDelta)
    {
        DockWaitSeconds += Math.Max(0, fixedDelta);
        if (_dockWaitingHarvesters.Add(harvesterId))
        {
            RefineryCongestionEvents++;
        }
    }

    public void ClearDockWait(int harvesterId)
    {
        _dockWaitingHarvesters.Remove(harvesterId);
    }

    public void RecordResourceTripCompleted()
    {
        ResourceTripCompletions++;
    }

    public void RecordMovementSample(int entityId, Vector2 previous, Vector2 current, Vector2 target, double fixedDelta)
    {
        var delta = current - previous;
        var travel = delta.Length();
        var previousRemaining = previous.DistanceTo(target);
        var currentRemaining = current.DistanceTo(target);
        MovementTravelDistance += travel;
        MovementDirectProgressDistance += Math.Max(0, previousRemaining - currentRemaining);

        if (_lastMoveTargetByEntity.TryGetValue(entityId, out var lastTarget)
            && lastTarget.DistanceSquaredTo(target) > 1f)
        {
            MovementRepathCount++;
            _lastMoveDirectionByEntity.Remove(entityId);
        }

        if (travel > 0.01f)
        {
            var direction = delta / travel;
            if (_lastMoveDirectionByEntity.TryGetValue(entityId, out var lastDirection)
                && MathF.Abs(Mathf.AngleDifference(lastDirection.Angle(), direction.Angle())) > 0.35f)
            {
                MovementCornerCount++;
            }

            _lastMoveDirectionByEntity[entityId] = direction;
        }
        else if (previousRemaining > 4)
        {
            MovementStuckSeconds += Math.Max(0, fixedDelta);
        }

        _lastMoveTargetByEntity[entityId] = target;
    }

    public void RecordMovementArrival(int entityId, double jitterDistance)
    {
        ArrivalSamples++;
        ArrivalJitterDistance += Math.Max(0, jitterDistance);
        _lastMoveDirectionByEntity.Remove(entityId);
        _lastMoveTargetByEntity.Remove(entityId);
    }

    public void RecordMovementIdle(int entityId)
    {
        _lastMoveDirectionByEntity.Remove(entityId);
        _lastMoveTargetByEntity.Remove(entityId);
    }

    public void RecordCompactnessSample(IReadOnlyList<Vector2> positions)
    {
        if (positions.Count == 0)
        {
            return;
        }

        var center = Vector2.Zero;
        foreach (var position in positions)
        {
            center += position;
        }

        center /= positions.Count;
        var totalRadius = 0.0;
        var maxRadius = 0.0;
        foreach (var position in positions)
        {
            var radius = position.DistanceTo(center);
            totalRadius += radius;
            maxRadius = Math.Max(maxRadius, radius);
        }

        CompactnessSamples++;
        CompactnessRadiusTotal += totalRadius / positions.Count;
        MaxCompactnessRadius = Math.Max(MaxCompactnessRadius, maxRadius);
    }

    public void RecordAttackTarget(int entityId, int targetId)
    {
        if (targetId <= 0)
        {
            return;
        }

        if (_lastAttackTargetByEntity.TryGetValue(entityId, out var previousTarget) && previousTarget != targetId)
        {
            TargetSwitchCount++;
        }

        _lastAttackTargetByEntity[entityId] = targetId;
    }

    public void ClearAttackTarget(int entityId)
    {
        _lastAttackTargetByEntity.Remove(entityId);
    }

    public void RecordAnchorPushEvent()
    {
        AnchorPushEvents++;
    }
}
