using Godot;

namespace ProceduralRts.Core;

public static class FootprintVisualMath
{
    public static FootprintStyle StyleFor(UnitSpecRuntimeDescriptor descriptor)
    {
        return descriptor.MovementDomain switch
        {
            MovementDomain.Air => new FootprintStyle(
                FootprintMarkKind.Contrail,
                38,
                1.4f,
                1.4f,
                22,
                descriptor.Radius * 0.22f,
                new Color("#d8f7ff", 0.16f)),
            MovementDomain.Naval => new FootprintStyle(
                FootprintMarkKind.Wake,
                30,
                2.2f,
                1.3f,
                34,
                descriptor.Radius * 0.46f,
                new Color("#8fffe1", 0.18f)),
            _ => LandStyle(descriptor),
        };
    }

    private static FootprintStyle LandStyle(UnitSpecRuntimeDescriptor descriptor)
    {
        return descriptor.WeightClass switch
        {
            UnitWeightClass.Light => new FootprintStyle(
                FootprintMarkKind.Step,
                24,
                1.55f,
                1.25f,
                9,
                descriptor.Radius * 0.28f,
                new Color("#d8f7ff", 0.16f)),
            UnitWeightClass.Heavy => new FootprintStyle(
                FootprintMarkKind.TrackPlate,
                18,
                3.25f,
                4.2f,
                18,
                descriptor.Radius * 0.46f,
                new Color("#f6c55c", 0.13f)),
            _ => new FootprintStyle(
                FootprintMarkKind.TwinTread,
                19,
                2.45f,
                2.4f,
                16,
                descriptor.Radius * 0.42f,
                new Color("#59f1ff", 0.14f)),
        };
    }
}
