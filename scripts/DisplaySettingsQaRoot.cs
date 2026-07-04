using Godot;
using ProceduralRts.Core;

namespace ProceduralRts;

public partial class DisplaySettingsQaRoot : Node
{
    public override void _Ready()
    {
        try
        {
            CheckFrameRateMode(FrameRateMode.Off, 0);
            CheckFrameRateMode(FrameRateMode.VSync, 0);
            CheckFrameRateMode(FrameRateMode.Fps60, 60);
            CheckFrameRateMode(FrameRateMode.Fps144, 144);
            CheckOwnerColorPalette();
            CheckImpactScreenShake();
            GD.Print("Display settings QA passed: Off/VSync/60/144 apply MaxFps, physics ticks, owner color palette modes, and impact shake setting.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private static void CheckFrameRateMode(FrameRateMode mode, int expectedMaxFps)
    {
        DisplayAudioSettings.ApplyFrameRateMode(mode, persist: false);
        if (DisplayAudioSettings.FrameRate != mode)
        {
            throw new InvalidOperationException($"Expected frame-rate mode {mode}, got {DisplayAudioSettings.FrameRate}.");
        }

        if (Engine.MaxFps != expectedMaxFps)
        {
            throw new InvalidOperationException($"Expected Engine.MaxFps {expectedMaxFps} for {mode}, got {Engine.MaxFps}.");
        }

        if (Engine.PhysicsTicksPerSecond != 60)
        {
            throw new InvalidOperationException($"Expected Engine.PhysicsTicksPerSecond 60, got {Engine.PhysicsTicksPerSecond}.");
        }
    }

    private static void CheckOwnerColorPalette()
    {
        DisplayAudioSettings.ApplyOwnerColorPalette(OwnerColorPaletteMode.Standard, persist: false);
        var standardOne = SoftOldCityPalette.PlayerColor(PlayerSlotId.One);
        var standardTwo = SoftOldCityPalette.PlayerColor(PlayerSlotId.Two);

        DisplayAudioSettings.ApplyOwnerColorPalette(OwnerColorPaletteMode.ColorblindSafe, persist: false);
        var safeOne = SoftOldCityPalette.PlayerColor(PlayerSlotId.One);
        var safeTwo = SoftOldCityPalette.PlayerColor(PlayerSlotId.Two);
        var safeThree = SoftOldCityPalette.PlayerColor(PlayerSlotId.Three);
        var safeFour = SoftOldCityPalette.PlayerColor(PlayerSlotId.Four);

        if (DisplayAudioSettings.OwnerColors != OwnerColorPaletteMode.ColorblindSafe)
        {
            throw new InvalidOperationException($"Expected owner palette {OwnerColorPaletteMode.ColorblindSafe}, got {DisplayAudioSettings.OwnerColors}.");
        }

        if (safeOne == standardOne || safeTwo == standardTwo)
        {
            throw new InvalidOperationException("Expected colorblind-safe owner colors to differ from the standard palette.");
        }

        if (ColorDistance(safeOne, safeTwo) < 0.34f
            || ColorDistance(safeOne, safeThree) < 0.34f
            || ColorDistance(safeTwo, safeFour) < 0.34f
            || ColorDistance(safeThree, safeFour) < 0.34f)
        {
            throw new InvalidOperationException("Expected colorblind-safe owner colors to remain visually separated.");
        }
    }

    private static void CheckImpactScreenShake()
    {
        DisplayAudioSettings.ApplyImpactScreenShake(false, persist: false);
        if (DisplayAudioSettings.ImpactScreenShake)
        {
            throw new InvalidOperationException("Expected impact screen shake to be disabled.");
        }

        DisplayAudioSettings.ApplyImpactScreenShake(true, persist: false);
        if (!DisplayAudioSettings.ImpactScreenShake)
        {
            throw new InvalidOperationException("Expected impact screen shake to be enabled.");
        }
    }

    private static float ColorDistance(Color a, Color b)
    {
        var dr = a.R - b.R;
        var dg = a.G - b.G;
        var db = a.B - b.B;
        return MathF.Sqrt(dr * dr + dg * dg + db * db);
    }
}
