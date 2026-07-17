namespace ProceduralRts.MapAuthoring.Editor;

public static class MapAuthoringRegistrationState
{
    private static readonly HashSet<string> ActiveTypes = new(StringComparer.Ordinal);

    public static bool Active { get; private set; }
    public static int ActiveTypeCount { get; private set; }
    public static int ActiveInspectorCount { get; private set; }
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

    public static void End()
    {
        if (!Active)
        {
            return;
        }

        if (ActiveTypeCount != 0 || ActiveInspectorCount != 0)
        {
            throw new InvalidOperationException(
                $"Map Authoring teardown left {ActiveTypeCount} custom types and {ActiveInspectorCount} Inspectors active.");
        }

        Active = false;
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
