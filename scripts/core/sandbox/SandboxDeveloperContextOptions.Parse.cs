using System.Globalization;

namespace ProceduralRts.Core;

public static partial class SandboxDeveloperContextOptions
{
    public static bool TryParseRequest(string field, string value, out SandboxDeveloperContextRequest request)
    {
        request = default;
        switch (NormalizeKey(field))
        {
            case "owner":
            case "owner-id":
            case "player":
            case "player-slot":
            case "slot":
                if (TryParseOwner(value, out var ownerId))
                {
                    request = new SandboxDeveloperContextRequest(OwnerId: ownerId);
                    return true;
                }

                return false;
            case "faction":
                if (TryParseFaction(value, out var faction))
                {
                    request = new SandboxDeveloperContextRequest(Faction: faction);
                    return true;
                }

                return false;
            case "team":
            case "team-id":
                if (TryParseTeam(value, out var teamId))
                {
                    request = new SandboxDeveloperContextRequest(TeamId: teamId);
                    return true;
                }

                return false;
            case "relation":
                if (TryParseRelation(value, out var relation))
                {
                    request = new SandboxDeveloperContextRequest(Relation: relation);
                    return true;
                }

                return false;
            case "environment":
            case "atmosphere":
                if (TryParseEnvironment(value, out var environment))
                {
                    request = new SandboxDeveloperContextRequest(Environment: environment);
                    return true;
                }

                return false;
            case "time":
            case "time-scale":
            case "timescale":
                if (TryParseTimeScale(value, out var scale))
                {
                    request = new SandboxDeveloperContextRequest(TimeScale: scale);
                    return true;
                }

                return false;
            case "debug-overlay":
            case "debug-overlay-preset":
            case "overlay":
            case "overlay-preset":
                if (TryParseDebugOverlayPreset(value, out var preset))
                {
                    request = new SandboxDeveloperContextRequest(DebugOverlayPreset: preset);
                    return true;
                }

                return false;
            case "debug-overlay-toggle":
            case "overlay-toggle":
                if (TryParseDebugOverlayFlag(value, out var flag))
                {
                    request = new SandboxDeveloperContextRequest(DebugOverlayToggle: flag);
                    return true;
                }

                return false;
            default:
                return false;
        }
    }

    public static bool TryParseOwner(string value, out OwnerId ownerId)
    {
        ownerId = default;
        if (!TryParseBoundedNumber(value, ["owner-", "slot-", "player-", "p"], out var number))
        {
            return false;
        }

        var candidate = new OwnerId(number);
        if (!Owners.Any(option => option.OwnerId == candidate))
        {
            return false;
        }

        ownerId = candidate;
        return true;
    }

    public static bool TryParseFaction(string value, out UnitFactionId faction)
    {
        var key = NormalizeKey(value);
        foreach (var option in Factions)
        {
            if (option.Key == key || string.Equals(option.Faction.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                faction = option.Faction;
                return true;
            }
        }

        faction = default;
        return false;
    }

    public static bool TryParseTeam(string value, out int teamId)
    {
        teamId = default;
        if (!TryParseBoundedNumber(value, ["team-"], out var number))
        {
            return false;
        }

        if (!Teams.Any(option => option.TeamId == number))
        {
            return false;
        }

        teamId = number;
        return true;
    }

    public static bool TryParseRelation(string value, out PlayerRelation relation)
    {
        var key = NormalizeKey(value);
        foreach (var option in Relations)
        {
            if (option.Key == key || string.Equals(option.Relation.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                relation = option.Relation;
                return true;
            }
        }

        relation = default;
        return false;
    }

    public static bool TryParseEnvironment(string value, out SandboxAtmospherePreset preset)
    {
        var key = NormalizeKey(value);
        foreach (var option in Environments)
        {
            if (option.Key == key || string.Equals(option.Preset.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                preset = option.Preset;
                return true;
            }
        }

        preset = default;
        return false;
    }

    public static bool TryParseTimeScale(string value, out float scale)
    {
        scale = default;
        var key = NormalizeKey(value);
        var numeric = key.TrimStart('x').TrimEnd('x');
        if (!float.TryParse(numeric, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        var normalized = NormalizeTimeScale(parsed);
        if (MathF.Abs(normalized - parsed) > 0.0001f)
        {
            return false;
        }

        scale = normalized;
        return true;
    }

    public static bool TryParseDebugOverlayPreset(string value, out SandboxDebugOverlayPreset preset)
    {
        var key = NormalizeKey(value);
        foreach (var candidate in SandboxDebugOverlayState.Presets)
        {
            if (candidate.Key == key)
            {
                preset = candidate;
                return true;
            }
        }

        preset = default;
        return false;
    }

    public static bool TryParseDebugOverlayFlag(string value, out SandboxDebugOverlayFlag flag)
    {
        var key = NormalizeKey(value);
        foreach (var entry in SandboxDebugOverlayState.Entries)
        {
            if (entry.Key == key || string.Equals(entry.Flag.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                flag = entry.Flag;
                return true;
            }
        }

        flag = SandboxDebugOverlayFlag.None;
        return false;
    }

    private static bool TryParseBoundedNumber(string value, IReadOnlyList<string> prefixes, out int number)
    {
        var key = NormalizeKey(value);
        foreach (var prefix in prefixes)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                key = key[prefix.Length..];
                break;
            }
        }

        return int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out number);
    }

    private static string TimeScaleKey(float scale)
    {
        return $"{scale.ToString("0.##", CultureInfo.InvariantCulture)}x";
    }

    private static string NormalizeKey(string value)
    {
        return value.Trim().ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
    }
}
