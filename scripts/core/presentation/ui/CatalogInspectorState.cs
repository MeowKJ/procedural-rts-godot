namespace ProceduralRts.Core;

public enum CatalogInspectorIntentKind
{
    ResetContext,
    SetDefault,
    SetHover,
    ClearHover,
    SetPin,
    ClearPin,
    SetCommandFeedback,
    ClearCommandFeedback,
    RefreshItem,
    InvalidateItem,
}

public enum CatalogInspectorLayer
{
    Default,
    Hover,
    Pin,
    CommandFeedback,
}

public readonly record struct CatalogInspectorContent(string ItemId, string Text)
{
    public bool IsVisible => !string.IsNullOrWhiteSpace(Text);
}

public readonly record struct CatalogInspectorState(
    CatalogInspectorContent Default,
    CatalogInspectorContent? Hover,
    CatalogInspectorContent? Pin,
    CatalogInspectorContent? CommandFeedback)
{
    public static CatalogInspectorState Empty { get; } = new(new("catalog:none", ""), null, null, null);

    public CatalogInspectorResolvedState Resolved =>
        ResolvedContent(CatalogInspectorLayer.CommandFeedback, CommandFeedback)
        ?? ResolvedContent(CatalogInspectorLayer.Pin, Pin)
        ?? ResolvedContent(CatalogInspectorLayer.Hover, Hover)
        ?? new CatalogInspectorResolvedState(CatalogInspectorLayer.Default, Default);

    public CatalogInspectorContent Current => Resolved.Content;

    private static CatalogInspectorResolvedState? ResolvedContent(
        CatalogInspectorLayer layer,
        CatalogInspectorContent? content)
    {
        return content is { IsVisible: true } value
            ? new CatalogInspectorResolvedState(layer, value)
            : null;
    }
}

public readonly record struct CatalogInspectorResolvedState(
    CatalogInspectorLayer Layer,
    CatalogInspectorContent Content);

public readonly record struct CatalogInspectorIntent(
    CatalogInspectorIntentKind Kind,
    string ItemId = "",
    string Text = "")
{
    public static CatalogInspectorIntent Reset(string contextId, string text) =>
        new(CatalogInspectorIntentKind.ResetContext, contextId, text);

    public static CatalogInspectorIntent Default(string contextId, string text) =>
        new(CatalogInspectorIntentKind.SetDefault, contextId, text);

    public static CatalogInspectorIntent Hover(string itemId, string text) =>
        new(CatalogInspectorIntentKind.SetHover, itemId, text);

    public static CatalogInspectorIntent ClearHover(string itemId) =>
        new(CatalogInspectorIntentKind.ClearHover, itemId);

    public static CatalogInspectorIntent Pin(string itemId, string text) =>
        new(CatalogInspectorIntentKind.SetPin, itemId, text);

    public static CatalogInspectorIntent ClearPin() =>
        new(CatalogInspectorIntentKind.ClearPin);

    public static CatalogInspectorIntent CommandFeedback(string text) =>
        new(CatalogInspectorIntentKind.SetCommandFeedback, "command-feedback", text);

    public static CatalogInspectorIntent ClearCommandFeedback() =>
        new(CatalogInspectorIntentKind.ClearCommandFeedback);

    public static CatalogInspectorIntent Refresh(string itemId, string text) =>
        new(CatalogInspectorIntentKind.RefreshItem, itemId, text);

    public static CatalogInspectorIntent Invalidate(string itemId) =>
        new(CatalogInspectorIntentKind.InvalidateItem, itemId);
}

public static class CatalogInspectorReducer
{
    public static CatalogInspectorState Apply(CatalogInspectorState state, CatalogInspectorIntent intent)
    {
        return intent.Kind switch
        {
            CatalogInspectorIntentKind.ResetContext => new CatalogInspectorState(
                new CatalogInspectorContent(intent.ItemId, intent.Text),
                null,
                null,
                null),
            CatalogInspectorIntentKind.SetDefault => state with
            {
                Default = new CatalogInspectorContent(intent.ItemId, intent.Text),
            },
            CatalogInspectorIntentKind.SetHover => state with
            {
                Hover = ContentOrNull(intent),
            },
            CatalogInspectorIntentKind.ClearHover => state.Hover is { } hover
                && string.Equals(hover.ItemId, intent.ItemId, StringComparison.Ordinal)
                    ? state with { Hover = null }
                    : state,
            CatalogInspectorIntentKind.SetPin => state with
            {
                Pin = ContentOrNull(intent),
            },
            CatalogInspectorIntentKind.ClearPin => state with { Pin = null },
            CatalogInspectorIntentKind.SetCommandFeedback => state with
            {
                CommandFeedback = ContentOrNull(intent),
            },
            CatalogInspectorIntentKind.ClearCommandFeedback => state with { CommandFeedback = null },
            CatalogInspectorIntentKind.RefreshItem => state with
            {
                Hover = WithUpdatedText(state.Hover, intent),
                Pin = WithUpdatedText(state.Pin, intent),
            },
            CatalogInspectorIntentKind.InvalidateItem => state with
            {
                Hover = WithoutItem(state.Hover, intent.ItemId),
                Pin = WithoutItem(state.Pin, intent.ItemId),
            },
            _ => state,
        };
    }

    private static CatalogInspectorContent? ContentOrNull(CatalogInspectorIntent intent)
    {
        return string.IsNullOrWhiteSpace(intent.Text)
            ? null
            : new CatalogInspectorContent(intent.ItemId, intent.Text);
    }

    private static CatalogInspectorContent? WithoutItem(CatalogInspectorContent? content, string itemId)
    {
        return content is { } value
            && string.Equals(value.ItemId, itemId, StringComparison.Ordinal)
                ? null
                : content;
    }

    private static CatalogInspectorContent? WithUpdatedText(
        CatalogInspectorContent? content,
        CatalogInspectorIntent intent)
    {
        return content is { } value
            && string.Equals(value.ItemId, intent.ItemId, StringComparison.Ordinal)
                ? ContentOrNull(intent)
                : content;
    }
}
