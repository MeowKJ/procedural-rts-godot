namespace ProceduralRts.Core;

[Flags]
public enum SandboxDebugOverlayFlag
{
    None = 0,
    Paths = 1 << 0,
    Slots = 1 << 1,
    Avoidance = 1 << 2,
    Rings = 1 << 3,
    Anchors = 1 << 4,
    Components = 1 << 5,
    CommandLog = 1 << 6,
    StateHash = 1 << 7,
    All = Paths
        | Slots
        | Avoidance
        | Rings
        | Anchors
        | Components
        | CommandLog
        | StateHash,
}

public readonly record struct SandboxDebugOverlayEntry(
    SandboxDebugOverlayFlag Flag,
    string Key,
    string Label);

public readonly record struct SandboxDebugOverlayPreset(
    string Key,
    string Label,
    SandboxDebugOverlayFlag Flags);

public readonly record struct SandboxDebugOverlayState(SandboxDebugOverlayFlag EnabledFlags)
{
    public static SandboxDebugOverlayState Empty { get; } = new(SandboxDebugOverlayFlag.None);

    public static readonly IReadOnlyList<SandboxDebugOverlayEntry> Entries =
    [
        new(SandboxDebugOverlayFlag.Paths, "paths", "Paths"),
        new(SandboxDebugOverlayFlag.Slots, "slots", "Slots"),
        new(SandboxDebugOverlayFlag.Avoidance, "avoidance", "Avoidance"),
        new(SandboxDebugOverlayFlag.Rings, "rings", "Rings"),
        new(SandboxDebugOverlayFlag.Anchors, "anchors", "Anchors"),
        new(SandboxDebugOverlayFlag.Components, "components", "Components"),
        new(SandboxDebugOverlayFlag.CommandLog, "command-log", "Command log"),
        new(SandboxDebugOverlayFlag.StateHash, "state-hash", "State hash"),
    ];

    public static readonly IReadOnlyList<SandboxDebugOverlayPreset> Presets =
    [
        new("off", "Off", SandboxDebugOverlayFlag.None),
        new("movement", "Movement", SandboxDebugOverlayFlag.Paths
            | SandboxDebugOverlayFlag.Slots
            | SandboxDebugOverlayFlag.Avoidance
            | SandboxDebugOverlayFlag.Rings
            | SandboxDebugOverlayFlag.Anchors),
        new("diagnostics", "Diagnostics", SandboxDebugOverlayFlag.Components
            | SandboxDebugOverlayFlag.CommandLog
            | SandboxDebugOverlayFlag.StateHash),
        new("all", "All", SandboxDebugOverlayFlag.All),
    ];

    public SandboxDebugOverlayState Toggle(SandboxDebugOverlayFlag flag)
    {
        var normalized = Normalize(flag);
        return new SandboxDebugOverlayState(EnabledFlags ^ normalized);
    }

    public SandboxDebugOverlayState Set(SandboxDebugOverlayFlag flag, bool enabled)
    {
        var normalized = Normalize(flag);
        var next = enabled
            ? EnabledFlags | normalized
            : EnabledFlags & ~normalized;
        return new SandboxDebugOverlayState(Normalize(next));
    }

    public SandboxDebugOverlayState ApplyPreset(SandboxDebugOverlayPreset preset)
    {
        return new SandboxDebugOverlayState(Normalize(preset.Flags));
    }

    public bool IsEnabled(SandboxDebugOverlayFlag flag)
    {
        var normalized = Normalize(flag);
        return normalized != SandboxDebugOverlayFlag.None
            && (EnabledFlags & normalized) == normalized;
    }

    public string FormatStatus()
    {
        var enabledFlags = Normalize(EnabledFlags);
        var enabled = Entries
            .Where(entry => (enabledFlags & entry.Flag) == entry.Flag)
            .Select(entry => entry.Key)
            .ToArray();

        return enabled.Length == 0
            ? "Sandbox overlays: off"
            : $"Sandbox overlays: {string.Join(", ", enabled)}";
    }

    public static string FormatLabel(SandboxDebugOverlayFlag flag)
    {
        var normalized = Normalize(flag);
        if (normalized == SandboxDebugOverlayFlag.None)
        {
            return "Off";
        }

        var exact = Entries.FirstOrDefault(entry => entry.Flag == normalized);
        if (exact.Flag == normalized)
        {
            return exact.Label;
        }

        return string.Join(
            ", ",
            Entries
                .Where(entry => (normalized & entry.Flag) == entry.Flag)
                .Select(entry => entry.Label));
    }

    public string FormatStatus(SandboxDebugOverlayFlag flag)
    {
        return $"{FormatLabel(flag)}: {(IsEnabled(flag) ? "on" : "off")}";
    }

    public static SandboxDebugOverlayPreset PresetByKey(string key)
    {
        return Presets.FirstOrDefault(
            preset => string.Equals(preset.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    private static SandboxDebugOverlayFlag Normalize(SandboxDebugOverlayFlag flag)
    {
        return flag & SandboxDebugOverlayFlag.All;
    }
}
