using Godot;

namespace ProceduralRts.Core;

public sealed record UnitRenderPalette(
    Color Body,
    Color Outline,
    Color Faction,
    Color Player,
    Color Warning,
    Color Selection,
    Color Effect)
{
    public static UnitRenderPalette SoftOldCity(UnitFactionId faction, PlayerSlotId playerSlotId)
    {
        return SoftOldCity(SoftOldCityPalette.FactionColor(faction), SoftOldCityPalette.PlayerColor(playerSlotId));
    }

    public static UnitRenderPalette SoftOldCity(Color faction, Color player)
    {
        return new UnitRenderPalette(
            new Color(SoftOldCityPalette.PaperSubtle, 0.86f),
            new Color(SoftOldCityPalette.Ink, 0.92f),
            new Color(faction, 0.82f),
            new Color(player, 0.84f),
            new Color(SoftOldCityPalette.Danger, 0.86f),
            new Color(SoftOldCityPalette.InnerLight, 0.72f),
            new Color(SoftOldCityPalette.PaperStrong, 0.50f));
    }

    public Color Resolve(ColorRole colorRole)
    {
        return Resolve(colorRole, null);
    }

    public Color Resolve(
        ColorRole colorRole,
        EnvironmentTone? tone,
        EnvironmentResponse response = EnvironmentResponse.Normal)
    {
        var color = colorRole switch
        {
            ColorRole.Body => Body,
            ColorRole.Ink => Outline,
            ColorRole.Owner => Player,
            ColorRole.Warning => Warning,
            ColorRole.Effect => Effect,
            ColorRole.Shadow => Selection,
            _ => Effect,
        };

        return tone is null ? color : tone.Apply(color, colorRole, response);
    }
}
