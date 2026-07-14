namespace ProceduralRts.Core;

public static class CommandFailurePresentation
{
    private static readonly string[] FailureStatusKeys =
    [
        "ui.needCredits",
        "ui.producerUnavailable",
        "ui.ability.unavailable",
        "stance.selectRequired",
        "build.cannotPlace",
        "build.noTicket",
        "build.sell.none",
        "harvest.selectHarvester",
        "harvest.depleted",
        "harvest.needRefinery",
        "rally.selectProducer",
        "rally.unsupported",
        "production.needProducer",
        "production.needCredits",
        "production.noneQueued",
        "ui.commandFailure.invalidTarget",
        "ui.commandFailure.invalidSelection",
        "ui.commandFailure.tooManyUnits",
        "ui.commandFailure.unavailable",
        "ui.commandFailure.stale",
        "ui.commandFailure.invalidCommand",
        "ui.commandFailure.notAuthorized",
    ];

    public static bool IsFailureStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        for (var index = 0; index < FailureStatusKeys.Length; index++)
        {
            if (MatchesLocalizedTemplate(status, FailureStatusKeys[index]))
            {
                return true;
            }
        }

        return false;
    }

    public static string InlineText(string status)
    {
        return IsFailureStatus(status)
            ? GameText.Format("ui.commandFailure.inline", status)
            : status;
    }

    public static string PanelText(string status)
    {
        return IsFailureStatus(status)
            ? GameText.Format("ui.commandFailure.reason", status)
            : status;
    }

    private static bool MatchesLocalizedTemplate(string status, string key)
    {
        var template = GameText.T(key);
        var firstPlaceholder = template.IndexOf('{', StringComparison.Ordinal);
        if (firstPlaceholder < 0)
        {
            return string.Equals(status, template, StringComparison.Ordinal);
        }

        var lastPlaceholderEnd = template.LastIndexOf('}');
        if (lastPlaceholderEnd < firstPlaceholder)
        {
            return false;
        }

        var prefix = template[..firstPlaceholder];
        var suffix = template[(lastPlaceholderEnd + 1)..];
        if (status.Length < prefix.Length + suffix.Length)
        {
            return false;
        }

        return (prefix.Length == 0 || status.StartsWith(prefix, StringComparison.Ordinal))
            && (suffix.Length == 0 || status.EndsWith(suffix, StringComparison.Ordinal))
            && (prefix.Length > 0 || suffix.Length > 0);
    }
}
