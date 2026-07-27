using ProceduralRts.Core;

static class UnitStanceStripQa
{
    public static void AssertProjectionAndSource(string repoRoot)
    {
        var none = UnitStanceStripProjection.FromSelection(UnitStance.Hold, selectedUnitCount: 0);
        Require(none == UnitStanceStripProjection.None, "Zero selection must normalize to the empty stance projection.");
        Require(none.State == UnitStanceStripSelectionState.None && !none.IsSelected(UnitStance.Hold),
            "Zero selection must not highlight a stance even when a stale stance value is supplied.");

        var mixed = UnitStanceStripProjection.FromSelection(selectedStance: null, selectedUnitCount: 3);
        Require(mixed.State == UnitStanceStripSelectionState.Mixed && mixed.SelectedUnitCount == 3,
            "Mixed selection must preserve its selected-unit count.");
        Require(!UnitStancePresentationCatalog.Definitions.Any(item => mixed.IsSelected(item.Stance)),
            "Mixed selection must not highlight any stance button.");

        var uniformHold = UnitStanceStripProjection.FromSelection(UnitStance.Hold, selectedUnitCount: 2);
        Require(uniformHold.State == UnitStanceStripSelectionState.Uniform
            && uniformHold.IsSelected(UnitStance.Hold)
            && !uniformHold.IsSelected(UnitStance.Aggressive),
            "Uniform selection must highlight only its projected stance.");

        var projectionBeforeIntent = uniformHold;
        var intentCount = 0;
        UnitStance? requestedStance = null;
        Action<UnitStance> intentSink = stance =>
        {
            intentCount++;
            requestedStance = stance;
        };
        intentSink(UnitStance.Aggressive);
        Require(intentCount == 1 && requestedStance == UnitStance.Aggressive,
            "A stance press must emit exactly one typed intent.");
        Require(uniformHold == projectionBeforeIntent && uniformHold.IsSelected(UnitStance.Hold),
            "Hold must remain highlighted until a later authoritative projection arrives.");

        AssertControlSource(repoRoot);
    }

    private static void AssertControlSource(string repoRoot)
    {
        var strip = File.ReadAllText(Path.Combine(repoRoot, "scripts", "ui", "UnitStanceStrip.cs"));
        var hudControls = File.ReadAllText(Path.Combine(repoRoot, "scripts", "ui", "hud", "HudLayer.BuildControls.cs"));
        var pressEmission = "button.Pressed += () => IntentRequested?.Invoke(presentation.Stance);";

        Require(strip.Contains("public partial class UnitStanceStrip : Control", StringComparison.Ordinal),
            "The stance pilot must be a reusable top-level Godot Control.");
        Require(strip.Contains(pressEmission, StringComparison.Ordinal),
            "Stance buttons must emit their typed intent without a local state transition.");
        Require(CountOccurrences(strip, "IntentRequested?.Invoke") == 1,
            "The reusable stance strip must expose exactly one intent emission site.");
        Require(!strip.Contains("SetSelectedUnitStance", StringComparison.Ordinal)
            && !strip.Contains("UnitBattlefield", StringComparison.Ordinal)
            && !strip.Contains("SubmitLiveLocalPlayerCommand", StringComparison.Ordinal),
            "The stance strip must not read authority or submit commands directly.");
        Require(!hudControls.Contains("SetSelectedUnitStance(presentation.Stance", StringComparison.Ordinal),
            "The retired HudLayer button path must not restore optimistic stance highlighting.");
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var start = 0;
        while ((start = source.IndexOf(value, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += value.Length;
        }

        return count;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
