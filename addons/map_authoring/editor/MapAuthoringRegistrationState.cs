namespace ProceduralRts.MapAuthoring.Editor;

public static class MapAuthoringRegistrationState
{
    private static readonly HashSet<string> ActiveTypes = new(StringComparer.Ordinal);

    public static bool Active { get; private set; }
    public static int ActiveTypeCount { get; private set; }
    public static int ActiveInspectorCount { get; private set; }
    public static int ActiveFeatureCount { get; private set; }
    public static int ActiveDockCount { get; private set; }
    public static int ActiveForceDrawForwarderCount { get; private set; }
    public static int ForceDrawCallCount { get; private set; }
    public static int EnterCount { get; private set; }
    public static int ExitCount { get; private set; }
    public static IReadOnlyCollection<string> ActiveTypeNames => ActiveTypes;

    public static void Begin()
    {
        if (Active)
        {
            throw new InvalidOperationException("Map Authoring plugin registration is already active.");
        }

        Active = true;
        ActiveTypeCount = 0;
        ActiveInspectorCount = 0;
        ActiveFeatureCount = 0;
        ActiveDockCount = 0;
        ActiveForceDrawForwarderCount = 0;
        ForceDrawCallCount = 0;
        ActiveTypes.Clear();
        EnterCount++;
    }

    public static void TypeAdded(string name)
    {
        RequireActive();
        if (!ActiveTypes.Add(name))
        {
            throw new InvalidOperationException($"Map Authoring custom type '{name}' was registered twice.");
        }

        ActiveTypeCount++;
    }

    public static void TypeRemoved(string name)
    {
        RequireActive();
        if (!ActiveTypes.Remove(name))
        {
            throw new InvalidOperationException($"Map Authoring custom type '{name}' was not active during teardown.");
        }

        ActiveTypeCount--;
    }

    public static void InspectorAdded()
    {
        RequireActive();
        ActiveInspectorCount++;
    }

    public static void InspectorRemoved()
    {
        RequireActive();
        ActiveInspectorCount--;
    }

    public static void FeatureAdded() { RequireActive(); ActiveFeatureCount++; }
    public static void FeatureRemoved() { RequireActive(); ActiveFeatureCount--; }
    public static void DockAdded() { RequireActive(); ActiveDockCount++; }
    public static void DockRemoved() { RequireActive(); ActiveDockCount--; }

    public static void ForceDrawForwarderAdded()
    {
        RequireActive();
        if (ActiveForceDrawForwarderCount != 0)
            throw new InvalidOperationException("Map Authoring force draw forwarding was registered twice.");
        ActiveForceDrawForwarderCount = 1;
    }

    public static void ForceDrawForwarderRemoved()
    {
        RequireActive();
        if (ActiveForceDrawForwarderCount != 1)
            throw new InvalidOperationException("Map Authoring force draw forwarding teardown was unbalanced.");
        ActiveForceDrawForwarderCount = 0;
    }

    public static void ForceDrawForwarded()
    {
        RequireActive();
        if (ActiveForceDrawForwarderCount != 1)
            throw new InvalidOperationException("Force draw callback ran without one active forwarder.");
        ForceDrawCallCount++;
    }

    public static void End()
    {
        if (!Active)
        {
            return;
        }

        if (ActiveTypeCount != 0 || ActiveInspectorCount != 0 || ActiveFeatureCount != 0
            || ActiveDockCount != 0 || ActiveForceDrawForwarderCount != 0)
        {
            throw new InvalidOperationException(
                $"Map Authoring teardown left types={ActiveTypeCount}, inspectors={ActiveInspectorCount}, "
                + $"features={ActiveFeatureCount}, docks={ActiveDockCount}, force_draw={ActiveForceDrawForwarderCount}.");
        }

        Active = false;
        ForceDrawCallCount = 0;
        ActiveTypes.Clear();
        ExitCount++;
    }

    private static void RequireActive()
    {
        if (!Active)
        {
            throw new InvalidOperationException("Map Authoring registration is not active.");
        }
    }
}
