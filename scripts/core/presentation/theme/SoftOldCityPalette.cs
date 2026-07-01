using Godot;

namespace ProceduralRts.Core;

public static class SoftOldCityPalette
{
    public static readonly Color Paper = new("#eadbc4");
    public static readonly Color PaperStrong = new("#f2e3ca");
    public static readonly Color PaperSubtle = new("#dcc9aa");
    public static readonly Color Ink = new("#211b16");
    public static readonly Color Text = new("#2b3032");
    public static readonly Color InkMuted = new("#5f635e");
    public static readonly Color TextDim = new("#7a7468");
    public static readonly Color Border = new("#6f665e");
    public static readonly Color InnerLight = new("#fff0d2");
    public static readonly Color WarmCommand = new("#c47719");
    public static readonly Color Repair = new("#3f8068");
    public static readonly Color Danger = new("#c15b6c");
    public static readonly Color HudDanger = new("#a83255");
    public static readonly Color Cargo = new("#b98232");
    public static readonly Color Route = new("#50439c");
    public static readonly Color Water = new("#a7beb7");

    public static readonly Color FogPaper = new("#d9d6c9");
    public static readonly Color FogPaperStrong = new("#e5e0d1");
    public static readonly Color FogPaperSubtle = new("#c9c8bb");
    public static readonly Color FogText = new("#303938");
    public static readonly Color FogMuted = new("#626b66");
    public static readonly Color FogDim = new("#788077");
    public static readonly Color FogBorder = new("#687067");
    public static readonly Color FogCommand = new("#b77d2d");
    public static readonly Color FogRoute = new("#5d58a2");
    public static readonly Color FogDanger = new("#93415e");
    public static readonly Color FogWater = new("#a3b7b5");

    public static readonly Color DuskPanel = new("#242b2b");
    public static readonly Color DuskPanelStrong = new("#303332");
    public static readonly Color DuskPanelSubtle = new("#171d1d");
    public static readonly Color DuskText = new("#eef1ec");
    public static readonly Color DuskTextMuted = new("#c6c4b8");
    public static readonly Color DuskTextDim = new("#97978e");
    public static readonly Color DuskLine = new("#e7c982");
    public static readonly Color DuskCommand = new("#f0a33c");
    public static readonly Color DuskRoute = new("#8f82ff");
    public static readonly Color DuskRepair = new("#7dbd9e");
    public static readonly Color DuskDanger = new("#ff5578");

    public static readonly Color NightBackground = new("#101410");
    public static readonly Color NightGround = new("#171d1a");
    public static readonly Color NightRadar = new("#8fffe1");
    public static readonly Color NightRadarSoft = new("#d8f7ff");
    public static readonly Color NightWater = new("#073238");
    public static readonly Color NightWaterEdge = new("#64f2ff");
    public static readonly Color NightMuted = new("#9fb7aa");

    public static readonly Color DogFaction = new("#64c7c7");
    public static readonly Color CatFaction = new("#c98293");
    public static readonly Color CorruptionFaction = new("#9d4259");

    public static readonly Color PlayerOne = new("#68a6c8");
    public static readonly Color PlayerTwo = new("#c86c68");
    public static readonly Color PlayerThree = new("#8abf74");
    public static readonly Color PlayerFour = new("#c5a45d");
    public static readonly Color ColorblindPlayerOne = new("#3f7fb5");
    public static readonly Color ColorblindPlayerTwo = new("#c65f26");
    public static readonly Color ColorblindPlayerThree = new("#008f73");
    public static readonly Color ColorblindPlayerFour = new("#d6b82e");
    public static readonly Color Neutral = new("#b7ad9c");

    public static Color FactionColor(UnitFactionId faction)
    {
        return faction switch
        {
            UnitFactionId.Dog => DogFaction,
            UnitFactionId.Cat => CatFaction,
            UnitFactionId.Corruption => CorruptionFaction,
            _ => WarmCommand,
        };
    }

    public static Color PlayerColor(PlayerSlotId playerSlotId)
    {
        return PlayerColor(playerSlotId, DisplayAudioSettings.OwnerColors);
    }

    public static Color PlayerColor(PlayerSlotId playerSlotId, OwnerColorPaletteMode mode)
    {
        return playerSlotId.Value switch
        {
            1 => mode == OwnerColorPaletteMode.ColorblindSafe ? ColorblindPlayerOne : PlayerOne,
            2 => mode == OwnerColorPaletteMode.ColorblindSafe ? ColorblindPlayerTwo : PlayerTwo,
            3 => mode == OwnerColorPaletteMode.ColorblindSafe ? ColorblindPlayerThree : PlayerThree,
            4 => mode == OwnerColorPaletteMode.ColorblindSafe ? ColorblindPlayerFour : PlayerFour,
            _ => Neutral,
        };
    }

    public static Color RelationColor(PlayerRelation relation)
    {
        return relation switch
        {
            PlayerRelation.Self => PlayerColor(PlayerSlotId.One),
            PlayerRelation.Allied => PlayerColor(PlayerSlotId.Three),
            PlayerRelation.Hostile => Danger,
            PlayerRelation.Neutral => Neutral,
            _ => Neutral,
        };
    }
}
