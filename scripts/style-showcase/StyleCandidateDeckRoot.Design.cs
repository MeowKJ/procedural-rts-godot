using Godot;

namespace ProceduralRts;

public partial class StyleCandidateDeckRoot
{
    private static StyleFamily[] Families()
    {
        return
        [
            new(
                "STYLE 1",
                "Soft Old City",
                "柔和旧城战术板",
                "最稳的主方向：低疲劳、旧城道路、暖色修复感，适合狗狗战役默认画面。",
                [
                    new("1A", "Old City Day", "默认战斗", new Color("#eadbc4"), new Color("#bca98d", 0.30f), new Color("#786d5e", 0.26f), new Color("#2b3032"), new Color("#c47719"), new Color("#50439c"), new Color("#a83255"), 0.02f, 0.68f, false, 0),
                    new("1B", "Fog Morning", "探索/失联", new Color("#d9d6c9"), new Color("#8d9488", 0.23f), new Color("#687067", 0.22f), new Color("#303938"), new Color("#b77d2d"), new Color("#5d58a2"), new Color("#93415e"), 0.16f, 0.56f, false, 0),
                    new("1C", "Dusk Defense", "防守危机", new Color("#293234"), new Color("#e7c982", 0.10f), new Color("#d8912f", 0.18f), new Color("#eef1ec"), new Color("#f0a33c"), new Color("#8f82ff"), new Color("#ff5578"), 0.05f, 0.90f, true, 0),
                ]),
            new(
                "STYLE 2",
                "Porcelain Command",
                "白瓷指挥台",
                "最干净高级：界面轻、底色白、单位更像作战棋子；压迫感要靠危机态补足。",
                [
                    new("2A", "Porcelain Day", "默认建设", new Color("#f2eee7"), new Color("#c8c6c1", 0.30f), new Color("#9a9690", 0.22f), new Color("#252b2d"), new Color("#bc7424"), new Color("#5b4aaa"), new Color("#a03450"), 0.00f, 0.58f, false, 1),
                    new("2B", "Cool Command", "沙盒/基地", new Color("#e8edf0"), new Color("#aab6bb", 0.24f), new Color("#76898f", 0.22f), new Color("#243039"), new Color("#b8732b"), new Color("#4c5cb2"), new Color("#9e3659"), 0.06f, 0.62f, false, 1),
                    new("2C", "Red Overlay", "警报覆盖", new Color("#ebe7df"), new Color("#bdb7ab", 0.22f), new Color("#a75c6d", 0.20f), new Color("#2d2d2e"), new Color("#c47a24"), new Color("#604fb0"), new Color("#c63358"), 0.02f, 0.80f, false, 1),
                ]),
            new(
                "STYLE 3",
                "Archive Map",
                "档案地图",
                "剧情味最强：像旧城市档案和战役地图，适合含蓄叙事，但科技感较弱。",
                [
                    new("3A", "Paper Campaign", "战役默认", new Color("#dfccaa"), new Color("#8b7355", 0.24f), new Color("#684f33", 0.20f), new Color("#352920"), new Color("#a75d19"), new Color("#4b3d7e"), new Color("#8d2941"), 0.04f, 0.56f, false, 2),
                    new("3B", "Ink Scouting", "暗线侦察", new Color("#d4c4a6"), new Color("#5e5140", 0.20f), new Color("#473a2d", 0.18f), new Color("#28231f"), new Color("#9c651e"), new Color("#3d427d"), new Color("#813846"), 0.12f, 0.52f, false, 2),
                    new("3C", "Burnt Front", "战线崩坏", new Color("#b9a285"), new Color("#4d4034", 0.18f), new Color("#7f3428", 0.20f), new Color("#241f1c"), new Color("#bd6a1c"), new Color("#554185"), new Color("#ae2f45"), 0.06f, 0.72f, false, 2),
                ]),
            new(
                "STYLE 4",
                "Repair Blueprint",
                "维修蓝图",
                "最贴狗狗主题：修路灯、重启设施、恢复信号都很自然；猫猫需要用暗线和斜向符号补个性。",
                [
                    new("4A", "Base Blueprint", "基地建造", new Color("#dce6e4"), new Color("#71949a", 0.24f), new Color("#3c6973", 0.22f), new Color("#1f3135"), new Color("#bc7b2a"), new Color("#4c57a8"), new Color("#a52d55"), 0.03f, 0.64f, false, 3),
                    new("4B", "Signal Restore", "信号恢复", new Color("#d6e2dc"), new Color("#759b8b", 0.24f), new Color("#487866", 0.20f), new Color("#22342f"), new Color("#d08c2b"), new Color("#5160aa"), new Color("#9b3353"), 0.04f, 0.76f, false, 3),
                    new("4C", "System Crisis", "系统危机", new Color("#16242a"), new Color("#78b6c0", 0.11f), new Color("#e14b70", 0.18f), new Color("#edf4f2"), new Color("#ffbd55"), new Color("#8d8cff"), new Color("#ff3f69"), 0.07f, 0.96f, true, 3),
                ]),
            new(
                "STYLE 5",
                "Garden Circuit",
                "庭院线路",
                "更柔软、更有生命感：适合猫猫暗线和废城自然恢复，但战争感要控制住。",
                [
                    new("5A", "Pale Garden", "默认轻战斗", new Color("#e5dfc8"), new Color("#a9ad87", 0.23f), new Color("#768257", 0.20f), new Color("#2e342a"), new Color("#c1842b"), new Color("#5d4c99"), new Color("#9d385a"), 0.06f, 0.58f, false, 4),
                    new("5B", "Overgrown Lane", "隐蔽路线", new Color("#d4dac4"), new Color("#7d966e", 0.23f), new Color("#5b7654", 0.20f), new Color("#253326"), new Color("#b78233"), new Color("#5654a0"), new Color("#8f4260"), 0.12f, 0.54f, false, 4),
                    new("5C", "Garden Night", "夜间渗透", new Color("#1f2924"), new Color("#9fc58e", 0.10f), new Color("#da617f", 0.16f), new Color("#eef1e8"), new Color("#e5a84b"), new Color("#9185ff"), new Color("#ff5578"), 0.08f, 0.88f, true, 4),
                ]),
            new(
                "STYLE 6",
                "Signal Glass",
                "信号玻璃",
                "最现代清透：适合桌面端精致 UI，单位和路径会很清楚，但需要避免变成普通科幻界面。",
                [
                    new("6A", "Clear Signal", "默认指挥", new Color("#e6ebe8"), new Color("#a4b8b6", 0.22f), new Color("#6d898b", 0.20f), new Color("#253136"), new Color("#be7928"), new Color("#5460b0"), new Color("#a83258"), 0.03f, 0.64f, false, 5),
                    new("6B", "Pearl Radar", "侦察状态", new Color("#dfe7e8"), new Color("#93b2bc", 0.23f), new Color("#5f8290", 0.22f), new Color("#20313a"), new Color("#c2842d"), new Color("#4f5fb0"), new Color("#9d3a60"), 0.10f, 0.62f, false, 5),
                    new("6C", "Black Glass", "通信崩溃", new Color("#121b20"), new Color("#80b8c8", 0.10f), new Color("#ff5578", 0.18f), new Color("#ecf2f0"), new Color("#ffc05b"), new Color("#918dff"), new Color("#ff416b"), 0.06f, 0.96f, true, 5),
                ]),
        ];
    }

    private sealed record StyleFamily(
        string Code,
        string Name,
        string ChineseName,
        string Note,
        VariantSpec[] Variants);

    private sealed record VariantSpec(
        string Code,
        string Name,
        string Role,
        Color Background,
        Color Grid,
        Color Major,
        Color Ink,
        Color Dog,
        Color Cat,
        Color Ai,
        float Haze,
        float CommandAlpha,
        bool Dark,
        int FamilyMood);
}
