using ProceduralRts.Core;

internal sealed record BattleHudVisualGateCase(
    string Scenario,
    BattleHudRuntimeStateKind State,
    string CaptureId,
    BattleHudCaptureResolution Resolution,
    string FileName,
    IReadOnlyList<string> RequiredControls,
    IReadOnlyList<string> RequiredSignals);

internal static class BattleHudVisualGate
{
    public static BattleHudVisualGateCase Validate(
        BattleHudRuntimeStateSpec state,
        BattleHudCaptureResolution resolution,
        List<string> failures)
    {
        if (state.CriticalControls.Count == 0
            || state.CriticalControls.Distinct().Count() != state.CriticalControls.Count)
        {
            failures.Add($"{state.CaptureId}: critical runtime controls must be non-empty and unique");
        }

        if (state.CriticalSignals.Count == 0
            || state.CriticalSignals.Distinct().Count() != state.CriticalSignals.Count)
        {
            failures.Add($"{state.CaptureId}: critical runtime signals must be non-empty and unique");
        }

        foreach (var signal in state.CriticalSignals)
        {
            if (!SignalMatchesProjection(signal, state.Projection))
            {
                failures.Add($"{state.CaptureId}: projection does not satisfy critical signal {signal}");
            }
        }

        return new BattleHudVisualGateCase(
            BattleHudRuntimeStateCatalog.Scenario,
            state.Kind,
            state.CaptureId,
            resolution,
            state.CaptureFileName(resolution),
            state.CriticalControls.Select(control => control.ToString()).ToArray(),
            state.CriticalSignals.Select(signal => signal.ToString()).ToArray());
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
