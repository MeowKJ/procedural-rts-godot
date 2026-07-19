using ProceduralRts.Core;

internal sealed record BattleHudVisualGateCase(
    string Scenario,
    BattleHudRuntimeStateKind State,
    string CaptureId,
    BattleHudCaptureResolution Resolution,
    string FileName,
    IReadOnlyList<string> RequiredControls,
    IReadOnlyList<string> RequiredSignals,
    IReadOnlyList<string> RequiredRelations);

internal sealed record BattleHudVisualExpectedCase(
    IReadOnlyList<BattleHudRuntimeControlId> Controls,
    IReadOnlyList<BattleHudRuntimeSignalId> Signals);

internal static class BattleHudVisualGate
{
    private static readonly IReadOnlyDictionary<BattleHudRuntimeStateKind, BattleHudVisualExpectedCase>
        ExpectedByState = new Dictionary<BattleHudRuntimeStateKind, BattleHudVisualExpectedCase>
        {
            [BattleHudRuntimeStateKind.Empty] = new(
                [
                    BattleHudRuntimeControlId.ResourceStrip,
                    BattleHudRuntimeControlId.MinimapCluster,
                    BattleHudRuntimeControlId.RightRail,
                    BattleHudRuntimeControlId.CommandRibbon,
                    BattleHudRuntimeControlId.StatusLabel,
                ],
                [BattleHudRuntimeSignalId.NoSelection, BattleHudRuntimeSignalId.Status]),
            [BattleHudRuntimeStateKind.UnitSelected] = new(
                [
                    BattleHudRuntimeControlId.ResourceStrip,
                    BattleHudRuntimeControlId.MinimapCluster,
                    BattleHudRuntimeControlId.RightRail,
                    BattleHudRuntimeControlId.CommandRibbon,
                    BattleHudRuntimeControlId.StatusLabel,
                    BattleHudRuntimeControlId.UnitDetailPanel,
                    BattleHudRuntimeControlId.SelectionTitleLabel,
                    BattleHudRuntimeControlId.SelectionMetaLabel,
                    BattleHudRuntimeControlId.SelectionStatsLabel,
                    BattleHudRuntimeControlId.SelectionDetailLabel,
                    BattleHudRuntimeControlId.UnitStanceStrip,
                    BattleHudRuntimeControlId.StanceHold,
                ],
                [
                    BattleHudRuntimeSignalId.SelectionDetail,
                    BattleHudRuntimeSignalId.UniformHoldStance,
                    BattleHudRuntimeSignalId.Status,
                ]),
            [BattleHudRuntimeStateKind.ProductionBuildingSelected] = ProductionExpectation(
                BattleHudRuntimeSignalId.ProductionReady),
            [BattleHudRuntimeStateKind.UnavailableLowResources] = ProductionExpectation(
                BattleHudRuntimeSignalId.ProductionBlocked),
            [BattleHudRuntimeStateKind.QueueProgress] = ProductionExpectation(
                BattleHudRuntimeSignalId.QueueProgress,
                BattleHudRuntimeSignalId.QueueCancel),
            [BattleHudRuntimeStateKind.Alert] = new(
                [
                    BattleHudRuntimeControlId.ResourceStrip,
                    BattleHudRuntimeControlId.MinimapCluster,
                    BattleHudRuntimeControlId.RightRail,
                    BattleHudRuntimeControlId.CommandRibbon,
                    BattleHudRuntimeControlId.StatusLabel,
                    BattleHudRuntimeControlId.AlertRow0,
                ],
                [BattleHudRuntimeSignalId.AlertPayload, BattleHudRuntimeSignalId.Status]),
        };

    private static readonly (string Marker, BattleHudRuntimeControlId First, BattleHudRuntimeControlId Second)[]
        ExpectedRelations =
        [
            ("owner-contains:ResourceStrip>StatusLabel", BattleHudRuntimeControlId.ResourceStrip, BattleHudRuntimeControlId.StatusLabel),
            ("owner-contains:CommandRibbon>UnitStanceStrip", BattleHudRuntimeControlId.CommandRibbon, BattleHudRuntimeControlId.UnitStanceStrip),
            ("owner-contains:UnitStanceStrip>StanceHold", BattleHudRuntimeControlId.UnitStanceStrip, BattleHudRuntimeControlId.StanceHold),
            ("owner-contains:UnitDetailPanel>SelectionTitleLabel", BattleHudRuntimeControlId.UnitDetailPanel, BattleHudRuntimeControlId.SelectionTitleLabel),
            ("owner-contains:UnitDetailPanel>SelectionMetaLabel", BattleHudRuntimeControlId.UnitDetailPanel, BattleHudRuntimeControlId.SelectionMetaLabel),
            ("owner-contains:UnitDetailPanel>SelectionStatsLabel", BattleHudRuntimeControlId.UnitDetailPanel, BattleHudRuntimeControlId.SelectionStatsLabel),
            ("owner-contains:UnitDetailPanel>SelectionDetailLabel", BattleHudRuntimeControlId.UnitDetailPanel, BattleHudRuntimeControlId.SelectionDetailLabel),
            ("owner-contains:RightRail>ProductionProviderLane0", BattleHudRuntimeControlId.RightRail, BattleHudRuntimeControlId.ProductionProviderLane0),
            ("owner-contains:RightRail>QueueMiniStack", BattleHudRuntimeControlId.RightRail, BattleHudRuntimeControlId.QueueMiniStack),
            ("owner-contains:RightRail>CancelProduction", BattleHudRuntimeControlId.RightRail, BattleHudRuntimeControlId.CancelProduction),
            ("owner-contains:ProductionPanel>ProductionCard", BattleHudRuntimeControlId.ProductionPanel, BattleHudRuntimeControlId.ProductionCard),
            ("owner-contains:ProductionPanel>QueueSummaryLabel", BattleHudRuntimeControlId.ProductionPanel, BattleHudRuntimeControlId.QueueSummaryLabel),
            ("forbidden-overlap:ResourceStrip>MinimapCluster", BattleHudRuntimeControlId.ResourceStrip, BattleHudRuntimeControlId.MinimapCluster),
            ("forbidden-overlap:MinimapCluster>RightRail", BattleHudRuntimeControlId.MinimapCluster, BattleHudRuntimeControlId.RightRail),
            ("forbidden-overlap:MinimapCluster>ProductionPanel", BattleHudRuntimeControlId.MinimapCluster, BattleHudRuntimeControlId.ProductionPanel),
            ("forbidden-overlap:ProductionPanel>UnitDetailPanel", BattleHudRuntimeControlId.ProductionPanel, BattleHudRuntimeControlId.UnitDetailPanel),
            ("forbidden-overlap:CommandRibbon>UnitDetailPanel", BattleHudRuntimeControlId.CommandRibbon, BattleHudRuntimeControlId.UnitDetailPanel),
            ("forbidden-overlap:CommandRibbon>ProductionPanel", BattleHudRuntimeControlId.CommandRibbon, BattleHudRuntimeControlId.ProductionPanel),
            ("forbidden-overlap:CommandRibbon>RightRail", BattleHudRuntimeControlId.CommandRibbon, BattleHudRuntimeControlId.RightRail),
            ("forbidden-overlap:ProductionPanel>RightRail", BattleHudRuntimeControlId.ProductionPanel, BattleHudRuntimeControlId.RightRail),
        ];

    public static BattleHudVisualGateCase Validate(
        BattleHudRuntimeStateSpec state,
        BattleHudCaptureResolution resolution,
        List<string> failures)
    {
        var expected = ExpectedByState[state.Kind];
        RequireExactSet(
            state.CriticalControls,
            expected.Controls,
            $"{state.CaptureId}: critical runtime controls",
            failures);
        RequireExactSet(
            state.CriticalSignals,
            expected.Signals,
            $"{state.CaptureId}: critical runtime signals",
            failures);

        foreach (var signal in expected.Signals)
        {
            if (!SignalMatchesProjection(signal, state.Projection))
            {
                failures.Add($"{state.CaptureId}: projection does not satisfy critical signal {signal}");
            }
        }

        var expectedControlSet = expected.Controls.ToHashSet();
        var expectedRelations = ExpectedRelations
            .Where(relation => expectedControlSet.Contains(relation.First)
                && expectedControlSet.Contains(relation.Second))
            .Select(relation => relation.Marker)
            .ToArray();
        return new BattleHudVisualGateCase(
            BattleHudRuntimeStateCatalog.Scenario,
            state.Kind,
            state.CaptureId,
            resolution,
            state.CaptureFileName(resolution),
            expected.Controls.Select(control => control.ToString()).ToArray(),
            expected.Signals.Select(signal => signal.ToString()).ToArray(),
            expectedRelations);
    }

    private static BattleHudVisualExpectedCase ProductionExpectation(
        params BattleHudRuntimeSignalId[] contextualSignals)
    {
        var signals = new List<BattleHudRuntimeSignalId>
        {
            BattleHudRuntimeSignalId.BuildingDetail,
        };
        signals.AddRange(contextualSignals);
        signals.Add(BattleHudRuntimeSignalId.Status);
        return new BattleHudVisualExpectedCase(
            [
                BattleHudRuntimeControlId.ResourceStrip,
                BattleHudRuntimeControlId.MinimapCluster,
                BattleHudRuntimeControlId.RightRail,
                BattleHudRuntimeControlId.CommandRibbon,
                BattleHudRuntimeControlId.StatusLabel,
                BattleHudRuntimeControlId.UnitDetailPanel,
                BattleHudRuntimeControlId.SelectionTitleLabel,
                BattleHudRuntimeControlId.SelectionMetaLabel,
                BattleHudRuntimeControlId.SelectionStatsLabel,
                BattleHudRuntimeControlId.SelectionDetailLabel,
                BattleHudRuntimeControlId.ProductionPanel,
                BattleHudRuntimeControlId.QueueSummaryLabel,
                BattleHudRuntimeControlId.ProductionProviderLane0,
                BattleHudRuntimeControlId.ProductionCard,
                BattleHudRuntimeControlId.QueueMiniStack,
                BattleHudRuntimeControlId.CancelProduction,
            ],
            signals);
    }

    private static void RequireExactSet<T>(
        IReadOnlyList<T> actual,
        IReadOnlyList<T> expected,
        string description,
        List<string> failures)
        where T : notnull
    {
        var actualSet = actual.ToHashSet();
        var expectedSet = expected.ToHashSet();
        if (actualSet.Count != actual.Count
            || expectedSet.Count != expected.Count
            || !actualSet.SetEquals(expectedSet))
        {
            failures.Add($"{description} must exactly match the independent QA matrix");
        }
    }

    private static bool SignalMatchesProjection(
        BattleHudRuntimeSignalId signal,
        BattleHudRuntimeProjection projection) => signal switch
    {
        BattleHudRuntimeSignalId.NoSelection => projection.Selection.Kind == BattleHudSelectionKind.None,
        BattleHudRuntimeSignalId.Status => !string.IsNullOrWhiteSpace(projection.Status),
        BattleHudRuntimeSignalId.SelectionDetail => projection.Selection.Kind == BattleHudSelectionKind.Unit
            && !string.IsNullOrWhiteSpace(projection.Selection.Title),
        BattleHudRuntimeSignalId.UniformHoldStance => projection.StanceStrip.State == UnitStanceStripSelectionState.Uniform
            && projection.StanceStrip.IsSelected(UnitStance.Hold),
        BattleHudRuntimeSignalId.BuildingDetail => projection.Selection.Kind == BattleHudSelectionKind.ProductionBuilding
            && !string.IsNullOrWhiteSpace(projection.Selection.Detail),
        BattleHudRuntimeSignalId.ProductionReady => projection.Production is { Visible: true, EnoughCredits: true },
        BattleHudRuntimeSignalId.ProductionBlocked => projection.Production is { Visible: true, EnoughCredits: false }
            && !string.IsNullOrWhiteSpace(projection.Production.DisabledReasonKey),
        BattleHudRuntimeSignalId.QueueProgress => projection.Production is
            { QueuedCount: > 0, ActiveProgress: > 0 and < 1 },
        BattleHudRuntimeSignalId.QueueCancel => projection.Production.CanCancel,
        BattleHudRuntimeSignalId.AlertPayload => projection.Alert is { Text.Length: > 0, RemainingRatio: > 0 },
        _ => false,
    };
}
