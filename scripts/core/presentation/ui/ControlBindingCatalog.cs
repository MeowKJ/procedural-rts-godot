namespace ProceduralRts.Core;

public enum ControlBindingSectionKind
{
    Camera,
    Select,
    Orders,
    Stance,
    Groups,
    Build,
    Catalog,
    Debug,
}

public readonly record struct ControlBindingSection(
    ControlBindingSectionKind Kind,
    string TitleKey,
    IReadOnlyList<string> RowKeys);

public static class ControlBindingCatalog
{
    public static IReadOnlyList<ControlBindingSection> Sections { get; } =
    [
        new(ControlBindingSectionKind.Camera, "hotkeys.camera", ["hotkeys.camera.1", "hotkeys.camera.2", "hotkeys.camera.3"]),
        new(ControlBindingSectionKind.Select, "hotkeys.select", ["hotkeys.select.1", "hotkeys.select.2", "hotkeys.select.3"]),
        new(ControlBindingSectionKind.Orders, "hotkeys.orders", ["hotkeys.orders.1", "hotkeys.orders.2", "hotkeys.orders.3"]),
        new(ControlBindingSectionKind.Stance, "hotkeys.stance", ["hotkeys.stance.1", "hotkeys.stance.2", "hotkeys.stance.3"]),
        new(ControlBindingSectionKind.Groups, "hotkeys.groups", ["hotkeys.groups.1", "hotkeys.groups.2"]),
        new(ControlBindingSectionKind.Build, "hotkeys.build", ["hotkeys.build.1", "hotkeys.build.2", "hotkeys.build.3", "hotkeys.build.4"]),
        new(ControlBindingSectionKind.Catalog, "hotkeys.catalog", ["hotkeys.catalog.1", "hotkeys.catalog.2", "hotkeys.catalog.3"]),
        new(ControlBindingSectionKind.Debug, "hotkeys.debug", ["hotkeys.debug.1", "hotkeys.debug.2", "hotkeys.debug.3"]),
    ];

    public static IReadOnlyList<string> SettingsOverviewRowKeys { get; } =
    [
        "hotkeys.hint.open",
        "hotkeys.catalog.1",
        "hotkeys.build.4",
        "hotkeys.camera.3",
    ];
}
