using ProceduralRts.Core;
using ProceduralRts.Ui;

var palettes = new[]
{
    SoftOldCityTheme.For(WorldVisualTheme.DayCommand),
    SoftOldCityTheme.For(WorldVisualTheme.FogMorning),
    SoftOldCityTheme.For(WorldVisualTheme.DuskDefense),
    SoftOldCityTheme.For(WorldVisualTheme.NightRadar),
};
var primitives = Enum.GetValues<HudVisualPrimitive>();
var failures = new List<string>();

foreach (var palette in palettes)
{
    foreach (var primitive in primitives)
    {
        var normal = HudVisualFoundation.For(palette, primitive, HudVisualState.Normal, palette.Repair);
        var selected = HudVisualFoundation.For(palette, primitive, HudVisualState.Selected, palette.Repair);
        var disabled = HudVisualFoundation.For(palette, primitive, HudVisualState.Disabled, palette.Repair);
        var warning = HudVisualFoundation.For(palette, primitive, HudVisualState.Warning, palette.Repair);

        Require(selected.BorderWidth == 2 && selected.Border.A > normal.Border.A, $"{primitive} selected state must be visually stronger", failures);
        Require(disabled.Text.A < normal.Text.A && disabled.Border.A < normal.Border.A, $"{primitive} disabled state must be quieter", failures);
        Require(warning.Accent == palette.Danger && warning.Border.A > normal.Border.A, $"{primitive} warning state must use the danger token", failures);
    }
}

if (failures.Count > 0)
{
    throw new InvalidOperationException("HudVisualFoundationQa FAILED:\n" + string.Join("\n", failures));
}

Console.WriteLine("HudVisualFoundationQa PASSED: four HUD primitives expose coherent normal, selected, disabled, and warning tokens across every world theme.");

static void Require(bool condition, string message, List<string> failures)
{
    if (!condition)
    {
        failures.Add(message);
    }
}
