using Godot;

namespace ProceduralRts.Core;

public sealed record WorldThemePalette(
    Color Background,
    Color GridMinor,
    Color GridMajor,
    Color Boundary,
    Color GroundFill,
    Color GroundEdge,
    Color CommandFill,
    Color CommandEdge,
    Color NavigationFill,
    Color NavigationEdge,
    Color CoastFill,
    Color CoastEdge,
    Color WaterFill,
    Color WaterEdge,
    Color NavigationLine,
    Color StrataLine);
