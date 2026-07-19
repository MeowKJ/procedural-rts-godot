using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer
{
    private const float BattleHudRuntimeProbeTolerance = 1f;
    private const float BattleHudRuntimeSettledAlpha = 0.95f;

    private static readonly (BattleHudRuntimeControlId Child, BattleHudRuntimeControlId Owner)[]
        BattleHudRuntimeOwnedControls =
        [
            (BattleHudRuntimeControlId.UnitStanceStrip, BattleHudRuntimeControlId.CommandRibbon),
            (BattleHudRuntimeControlId.StanceHold, BattleHudRuntimeControlId.UnitStanceStrip),
            (BattleHudRuntimeControlId.StatusLabel, BattleHudRuntimeControlId.ResourceStrip),
            (BattleHudRuntimeControlId.SelectionTitleLabel, BattleHudRuntimeControlId.UnitDetailPanel),
            (BattleHudRuntimeControlId.SelectionMetaLabel, BattleHudRuntimeControlId.UnitDetailPanel),
            (BattleHudRuntimeControlId.SelectionStatsLabel, BattleHudRuntimeControlId.UnitDetailPanel),
            (BattleHudRuntimeControlId.SelectionDetailLabel, BattleHudRuntimeControlId.UnitDetailPanel),
            (BattleHudRuntimeControlId.ProductionProviderLane0, BattleHudRuntimeControlId.RightRail),
            (BattleHudRuntimeControlId.QueueMiniStack, BattleHudRuntimeControlId.RightRail),
            (BattleHudRuntimeControlId.CancelProduction, BattleHudRuntimeControlId.RightRail),
            (BattleHudRuntimeControlId.ProductionCard, BattleHudRuntimeControlId.ProductionPanel),
            (BattleHudRuntimeControlId.QueueSummaryLabel, BattleHudRuntimeControlId.ProductionPanel),
        ];

    private static readonly (BattleHudRuntimeControlId First, BattleHudRuntimeControlId Second)[]
        BattleHudRuntimeForbiddenOverlaps =
        [
            (BattleHudRuntimeControlId.ResourceStrip, BattleHudRuntimeControlId.MinimapCluster),
            (BattleHudRuntimeControlId.MinimapCluster, BattleHudRuntimeControlId.RightRail),
            (BattleHudRuntimeControlId.MinimapCluster, BattleHudRuntimeControlId.ProductionPanel),
            (BattleHudRuntimeControlId.ProductionPanel, BattleHudRuntimeControlId.UnitDetailPanel),
            (BattleHudRuntimeControlId.CommandRibbon, BattleHudRuntimeControlId.UnitDetailPanel),
            (BattleHudRuntimeControlId.CommandRibbon, BattleHudRuntimeControlId.ProductionPanel),
            (BattleHudRuntimeControlId.CommandRibbon, BattleHudRuntimeControlId.RightRail),
            (BattleHudRuntimeControlId.ProductionPanel, BattleHudRuntimeControlId.RightRail),
        ];

    public BattleHudRuntimeStructuralEvidence ProbeBattleHudRuntimeStructure(
        BattleHudRuntimeStateSpec state,
        BattleHudCaptureResolution resolution,
        string exactCommit,
        string captureRunNonce)
    {
        var checks = new List<string>();
        var expectedViewport = new Vector2(resolution.Width, resolution.Height);
        var viewport = GetViewport().GetVisibleRect();
        RequireRuntimeProbe(
            viewport.Size.DistanceTo(expectedViewport) <= BattleHudRuntimeProbeTolerance,
            "viewport-exact-resolution",
            state,
            resolution,
            checks);

        var rects = new Dictionary<BattleHudRuntimeControlId, Rect2>();
        var evidence = new List<BattleHudRuntimeControlEvidence>(state.CriticalControls.Count);
        foreach (var controlId in state.CriticalControls)
        {
            var control = ResolveBattleHudRuntimeControl(controlId);
            var rect = control.GetGlobalRect();
            var alpha = EffectiveAlpha(control);
            RequireRuntimeProbe(
                control.IsVisibleInTree(),
                $"visible:{controlId}",
                state,
                resolution,
                checks);
            RequireRuntimeProbe(
                alpha >= BattleHudRuntimeSettledAlpha,
                $"alpha:{controlId}",
                state,
                resolution,
                checks);
            RequireRuntimeProbe(
                rect.Size.X > 0 && rect.Size.Y > 0,
                $"nonzero:{controlId}",
                state,
                resolution,
                checks);
            RequireRuntimeProbe(
                Contains(viewport, rect, BattleHudRuntimeProbeTolerance),
                $"viewport-contains:{controlId}",
                state,
                resolution,
                checks);
            if (IsBattleHudRuntimeInteractiveControl(controlId))
            {
                RequireRuntimeProbe(
                    rect.Size.X >= HudLayoutMath.MinimumCommandHitTarget
                        && rect.Size.Y >= HudLayoutMath.MinimumCommandHitTarget,
                    $"hit-target:{controlId}",
                    state,
                    resolution,
                    checks);
            }
            if (control is Label label)
            {
                ValidateBattleHudRuntimeLabelFit(label, rect, controlId, state, resolution, checks);
            }

            rects.Add(controlId, rect);
            evidence.Add(new BattleHudRuntimeControlEvidence(
                controlId.ToString(),
                rect.Position.X,
                rect.Position.Y,
                rect.Size.X,
                rect.Size.Y,
                alpha));
        }

        foreach (var (child, owner) in BattleHudRuntimeOwnedControls)
        {
            if (!rects.TryGetValue(child, out var childRect)
                || !rects.TryGetValue(owner, out var ownerRect))
            {
                continue;
            }

            RequireRuntimeProbe(
                Contains(ownerRect, childRect, BattleHudRuntimeProbeTolerance),
                $"owner-contains:{owner}>{child}",
                state,
                resolution,
                checks,
                $"owner={FormatRect(ownerRect)} child={FormatRect(childRect)}");
        }

        foreach (var (first, second) in BattleHudRuntimeForbiddenOverlaps)
        {
            if (!rects.TryGetValue(first, out var firstRect)
                || !rects.TryGetValue(second, out var secondRect))
            {
                continue;
            }

            RequireRuntimeProbe(
                !Overlaps(firstRect, secondRect, BattleHudRuntimeProbeTolerance),
                $"forbidden-overlap:{first}>{second}",
                state,
                resolution,
                checks);
        }

        ValidateBattleHudRuntimeState(state, resolution, checks);
        return new BattleHudRuntimeStructuralEvidence(
            BattleHudRuntimeStateCatalog.Scenario,
            exactCommit,
            captureRunNonce,
            state.Kind.ToString(),
            state.CaptureId,
            state.CaptureFileName(resolution),
            resolution.Width,
            resolution.Height,
            Passed: true,
            checks,
            evidence);
    }

    private Control ResolveBattleHudRuntimeControl(BattleHudRuntimeControlId controlId) => controlId switch
    {
        BattleHudRuntimeControlId.ResourceStrip => GetNode<Control>("HudRoot/ResourceStrip"),
        BattleHudRuntimeControlId.MinimapCluster => GetNode<Control>("HudRoot/MinimapCluster"),
        BattleHudRuntimeControlId.RightRail => _rightRail,
        BattleHudRuntimeControlId.CommandRibbon => _commandRibbon,
        BattleHudRuntimeControlId.StatusLabel => _statusValue,
        BattleHudRuntimeControlId.UnitDetailPanel => _rightDetailPanel,
        BattleHudRuntimeControlId.SelectionTitleLabel => _drawerSelectedTitle,
        BattleHudRuntimeControlId.SelectionMetaLabel => _drawerSelectedMeta,
        BattleHudRuntimeControlId.SelectionStatsLabel => _drawerSelectedStats,
        BattleHudRuntimeControlId.SelectionDetailLabel => _drawerSelectedDetail,
        BattleHudRuntimeControlId.UnitStanceStrip => _unitStanceStrip,
        BattleHudRuntimeControlId.StanceHold => _unitStanceStrip.GetNode<BaseButton>("StanceHold"),
        BattleHudRuntimeControlId.ProductionPanel => _rightProductionPanel,
        BattleHudRuntimeControlId.QueueSummaryLabel => _queueValue,
        BattleHudRuntimeControlId.ProductionProviderLane0 => _productionProviderLaneButtons[0],
        BattleHudRuntimeControlId.ProductionCard => RuntimeProductionCard(),
        BattleHudRuntimeControlId.QueueMiniStack => _queueMiniStack,
        BattleHudRuntimeControlId.CancelProduction => _cancelProduction,
        BattleHudRuntimeControlId.AlertRow0 => _alertRows[0],
        _ => throw new ArgumentOutOfRangeException(nameof(controlId), controlId, null),
    };

    private CommandButton RuntimeProductionCard()
    {
        if (_visibleCommandCardStates.Count != 1)
        {
            throw new InvalidOperationException(
                $"Battle HUD runtime probe expected one visible production card, found {_visibleCommandCardStates.Count}.");
        }

        var optionId = ProductionOptionId(_visibleCommandCardStates[0]);
        return _commandButtons.TryGetValue(optionId, out var card)
            ? card
            : throw new InvalidOperationException($"Battle HUD runtime probe could not resolve production card {optionId}.");
    }

    private void ValidateBattleHudRuntimeState(
        BattleHudRuntimeStateSpec state,
        BattleHudCaptureResolution resolution,
        List<string> checks)
    {
        var projection = state.Projection;
        RequireRuntimeProbe(
            _creditsValue.Text == projection.Credits.ToString("N0"),
            "payload:credits",
            state,
            resolution,
            checks);
        RequireRuntimeProbe(
            _statusValue.Text == CompactText(projection.Status, 42),
            "payload:status",
            state,
            resolution,
            checks);
        MarkRuntimeSignal(BattleHudRuntimeSignalId.Status, checks);
        RequireRuntimeProbe(
            _drawerSelectedTitle.Text == CompactText(projection.Selection.Title, 24)
                && _drawerSelectedMeta.Text == CompactText(projection.Selection.Meta, 30)
                && _drawerSelectedStats.Text == CompactText(projection.Selection.Stats, 31)
                && _drawerSelectedDetail.Text == CompactText(projection.Selection.Detail, 34),
            "payload:selection-detail",
            state,
            resolution,
            checks);
        RequireRuntimeProbe(
            _rightDetailPanel.IsVisibleInTree() == (projection.Selection.Kind != BattleHudSelectionKind.None),
            "state:detail-drawer-visibility",
            state,
            resolution,
            checks);
        RequireRuntimeProbe(
            _rightProductionPanel.IsVisibleInTree() == projection.Production.Visible,
            "state:production-drawer-visibility",
            state,
            resolution,
            checks);
        switch (state.Kind)
        {
            case BattleHudRuntimeStateKind.Empty:
                MarkRuntimeSignal(BattleHudRuntimeSignalId.NoSelection, checks);
                break;
            case BattleHudRuntimeStateKind.UnitSelected:
                MarkRuntimeSignal(BattleHudRuntimeSignalId.SelectionDetail, checks);
                break;
            case BattleHudRuntimeStateKind.ProductionBuildingSelected:
            case BattleHudRuntimeStateKind.UnavailableLowResources:
            case BattleHudRuntimeStateKind.QueueProgress:
                MarkRuntimeSignal(BattleHudRuntimeSignalId.BuildingDetail, checks);
                break;
        }

        RequireRuntimeProbe(
            _unitStanceStrip.Projection == projection.StanceStrip,
            "state:stance-projection",
            state,
            resolution,
            checks);
        foreach (var stance in Enum.GetValues<UnitStance>())
        {
            RequireRuntimeProbe(
                _unitStanceStrip.IsButtonSelected(stance) == projection.StanceStrip.IsSelected(stance),
                $"state:stance-selected:{stance}",
                state,
                resolution,
                checks);
        }
        if (state.Kind == BattleHudRuntimeStateKind.UnitSelected)
        {
            MarkRuntimeSignal(BattleHudRuntimeSignalId.UniformHoldStance, checks);
        }

        ValidateBattleHudRuntimeAlert(state, resolution, checks);
        if (!projection.Production.Visible)
        {
            RequireRuntimeProbe(
                _visibleCommandCardStates.Count == 0 && _productionProviderLaneStates.Count == 0,
                "state:no-production-content",
                state,
                resolution,
                checks);
            return;
        }

        var production = projection.Production;
        var card = RuntimeProductionCard();
        var provider = _productionProviderLaneButtons[0];
        RequireRuntimeProbe(
            card.IsVisibleInTree()
                && card.Disabled == !production.EnoughCredits
                && card.QueuedCount == production.QueuedCount
                && Mathf.IsEqualApprox(card.ActiveProgress, production.ActiveProgress),
            "state:production-card",
            state,
            resolution,
            checks);
        RequireRuntimeProbe(
            provider.IsVisibleInTree()
                && !provider.Disabled
                && provider.State.Available
                && provider.State.QueueCount == production.QueuedCount
                && Mathf.IsEqualApprox(provider.State.ActiveProgress, production.ActiveProgress),
            "state:provider-lane",
            state,
            resolution,
            checks);
        RequireRuntimeProbe(
            _queueMiniStack.IsVisibleInTree()
                && _queueMiniStack.Available
                && _queueMiniStack.QueuedCount == production.QueuedCount
                && Mathf.IsEqualApprox(_queueMiniStack.ActiveProgress, production.ActiveProgress),
            "state:queue-stack",
            state,
            resolution,
            checks);
        RequireRuntimeProbe(
            _cancelProduction.IsVisibleInTree()
                && _cancelProduction.Disabled == !production.CanCancel,
            "state:cancel-affordance",
            state,
            resolution,
            checks);
        var queueSummaryLine = production.QueueSummary.Split('\n', 2)[0];
        RequireRuntimeProbe(
            _queueValue.Text == CompactText(queueSummaryLine, 28),
            "payload:queue-summary",
            state,
            resolution,
            checks);
        switch (state.Kind)
        {
            case BattleHudRuntimeStateKind.ProductionBuildingSelected:
                MarkRuntimeSignal(BattleHudRuntimeSignalId.ProductionReady, checks);
                break;
            case BattleHudRuntimeStateKind.UnavailableLowResources:
                MarkRuntimeSignal(BattleHudRuntimeSignalId.ProductionBlocked, checks);
                break;
            case BattleHudRuntimeStateKind.QueueProgress:
                MarkRuntimeSignal(BattleHudRuntimeSignalId.QueueProgress, checks);
                MarkRuntimeSignal(BattleHudRuntimeSignalId.QueueCancel, checks);
                break;
        }
    }

    private void ValidateBattleHudRuntimeAlert(
        BattleHudRuntimeStateSpec state,
        BattleHudCaptureResolution resolution,
        List<string> checks)
    {
        var expected = state.Projection.Alert;
        var actual = _alertRows[0].Alert;
        var matches = expected is null
            ? actual is null
            : actual is { } alert
                && alert.Kind == expected.Value.Kind
                && alert.Text == expected.Value.Text
                && Mathf.IsEqualApprox(alert.RemainingRatio, expected.Value.RemainingRatio);
        RequireRuntimeProbe(matches, "payload:alert", state, resolution, checks);
        if (state.Kind == BattleHudRuntimeStateKind.Alert)
        {
            MarkRuntimeSignal(BattleHudRuntimeSignalId.AlertPayload, checks);
        }
    }

    private static float EffectiveAlpha(CanvasItem control)
    {
        var alpha = 1f;
        Node? current = control;
        while (current is CanvasItem canvasItem)
        {
            alpha *= canvasItem.Modulate.A * canvasItem.SelfModulate.A;
            current = canvasItem.GetParent();
        }

        return alpha;
    }

    private static bool Contains(Rect2 owner, Rect2 child, float tolerance) =>
        child.Position.X >= owner.Position.X - tolerance
        && child.Position.Y >= owner.Position.Y - tolerance
        && child.Position.X + child.Size.X <= owner.Position.X + owner.Size.X + tolerance
        && child.Position.Y + child.Size.Y <= owner.Position.Y + owner.Size.Y + tolerance;

    private static bool Overlaps(Rect2 first, Rect2 second, float tolerance) =>
        first.Position.X < second.Position.X + second.Size.X - tolerance
        && first.Position.X + first.Size.X > second.Position.X + tolerance
        && first.Position.Y < second.Position.Y + second.Size.Y - tolerance
        && first.Position.Y + first.Size.Y > second.Position.Y + tolerance;

    private static bool IsBattleHudRuntimeInteractiveControl(BattleHudRuntimeControlId controlId) =>
        controlId is BattleHudRuntimeControlId.StanceHold
            or BattleHudRuntimeControlId.ProductionProviderLane0
            or BattleHudRuntimeControlId.ProductionCard
            or BattleHudRuntimeControlId.CancelProduction;

    private static void ValidateBattleHudRuntimeLabelFit(
        Label label,
        Rect2 allottedRect,
        BattleHudRuntimeControlId controlId,
        BattleHudRuntimeStateSpec state,
        BattleHudCaptureResolution resolution,
        List<string> checks)
    {
        var minimum = label.GetMinimumSize();
        var measured = MeasureBattleHudRuntimeLabelText(label);
        var marker = $"text-fit:{controlId}:minimum={minimum.X:0.##}x{minimum.Y:0.##};" +
            $"measured={measured.X:0.##}x{measured.Y:0.##};" +
            $"allotted={allottedRect.Size.X:0.##}x{allottedRect.Size.Y:0.##}";
        RequireRuntimeProbe(
            minimum.X <= allottedRect.Size.X + BattleHudRuntimeProbeTolerance
                && minimum.Y <= allottedRect.Size.Y + BattleHudRuntimeProbeTolerance
                && measured.X <= allottedRect.Size.X + BattleHudRuntimeProbeTolerance
                && measured.Y <= allottedRect.Size.Y + BattleHudRuntimeProbeTolerance,
            marker,
            state,
            resolution,
            checks);
    }

    private static Vector2 MeasureBattleHudRuntimeLabelText(Label label)
    {
        var settings = label.LabelSettings;
        var font = settings?.Font ?? label.GetThemeFont("font");
        var fontSize = settings?.FontSize ?? label.GetThemeFontSize("font_size");
        var lines = label.Text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var width = 0f;
        foreach (var line in lines)
        {
            width = Mathf.Max(width, font.GetStringSize(line, fontSize: fontSize).X);
        }

        var lineSpacing = label.GetThemeConstant("line_spacing");
        return new Vector2(
            width,
            font.GetHeight(fontSize) * lines.Length
                + lineSpacing * Math.Max(0, lines.Length - 1));
    }

    private static void MarkRuntimeSignal(BattleHudRuntimeSignalId signal, List<string> checks) =>
        checks.Add($"signal:{signal}");

    private static string FormatRect(Rect2 rect) =>
        $"{rect.Position.X:0.##},{rect.Position.Y:0.##} {rect.Size.X:0.##}x{rect.Size.Y:0.##}";

    private static void RequireRuntimeProbe(
        bool condition,
        string check,
        BattleHudRuntimeStateSpec state,
        BattleHudCaptureResolution resolution,
        List<string> checks,
        string? failureDetail = null)
    {
        if (!condition)
        {
            throw new InvalidOperationException(
                $"Battle HUD runtime probe failed {state.CaptureId}/{resolution.Suffix}: {check}" +
                (failureDetail is null ? "." : $" ({failureDetail})."));
        }

        checks.Add(check);
    }
}
