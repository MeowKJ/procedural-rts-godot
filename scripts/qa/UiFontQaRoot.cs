using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;
using System.Text;

namespace ProceduralRts;

public partial class UiFontQaRoot : Node
{
    public override void _Ready()
    {
        try
        {
            CheckProfileMetadata();
            CheckRoleCoverage();
            CheckFactoryControls();
            GD.Print("UI font QA passed: shared Latin/CJK font profile, factory controls, and representative English + Simplified Chinese glyph coverage.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private static void CheckProfileMetadata()
    {
        Require(UiFontProfile.ProfileName.Contains("LatinCjk", StringComparison.Ordinal), "Profile name must state Latin/CJK intent.");
        Require(UiFontProfile.FallbackOrder.Contains("Noto Sans CJK SC"), "Fallback order must include a Simplified Chinese CJK face.");
        Require(UiFontProfile.FallbackOrder.Contains("Inter"), "Fallback order must include a clean Latin UI face.");
        Require(UiFontProfile.ChineseCoverageSample.Any(IsCjk), "Chinese coverage sample must contain CJK characters.");
        Require(UiFontProfile.EnglishCoverageSample.Any(char.IsAsciiLetter), "English coverage sample must contain Latin characters.");
    }

    private static void CheckRoleCoverage()
    {
        var previousLanguage = GameText.CurrentLanguage;
        try
        {
            GameText.CurrentLanguage = GameLanguage.English;
            var english = string.Join(" ", UiFontProfile.EnglishCoverageSample, GameText.T("menu.title"), GameText.T("settings.title"), GameText.T("ui.queue.empty"));
            GameText.CurrentLanguage = GameLanguage.ChineseSimplified;
            var chinese = string.Join(" ", UiFontProfile.ChineseCoverageSample, GameText.T("menu.title"), GameText.T("settings.title"), GameText.T("ui.queue.empty"));

            foreach (var role in Enum.GetValues<UiFontRole>())
            {
                var font = UiFontProfile.FontFor(role);
                Require(font is not null, $"{role} font must not be null.");
                RequireGlyphCoverage(font!, role, english);
                RequireGlyphCoverage(font!, role, chinese);
            }
        }
        finally
        {
            GameText.CurrentLanguage = previousLanguage;
        }
    }

    private static void CheckFactoryControls()
    {
        var label = UiFactory.MakeLabel("简体中文 / English", 14, Colors.White);
        Require(label.LabelSettings?.Font is not null, "UiFactory.MakeLabel must attach the shared font profile.");

        var hudLabel = UiFactory.MakeHudLabel("队列 READY", Vector2.Zero, 11, Colors.White, SoftOldCityTheme.Day);
        Require(hudLabel.LabelSettings?.Font is not null, "UiFactory.MakeHudLabel must attach the shared font profile.");

        var button = UiFactory.MakeButton("设置 / Settings", Colors.White);
        Require(button.HasThemeFontOverride("font"), "UiFactory.MakeButton must attach the shared button font.");
    }

    private static void RequireGlyphCoverage(Font font, UiFontRole role, string text)
    {
        var missing = new List<string>();
        foreach (var rune in text.EnumerateRunes())
        {
            if (Rune.IsWhiteSpace(rune) || char.IsPunctuation((char)rune.Value))
            {
                continue;
            }

            if (!font.HasChar(rune.Value))
            {
                missing.Add(rune.ToString());
            }
        }

        Require(missing.Count == 0, $"{role} font is missing glyphs: {string.Join(" ", missing.Distinct())}");
    }

    private static bool IsCjk(char value)
    {
        return value is >= '\u4e00' and <= '\u9fff';
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
