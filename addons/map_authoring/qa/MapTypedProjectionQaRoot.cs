using Godot;

namespace ProceduralRts.MapAuthoring.Qa;

public partial class MapTypedProjectionQaRoot : Node
{
    public override void _Ready()
    {
        try
        {
            MapTypedProjectionScenarios.Run();
            GD.Print("Map typed projection QA PASSED: ten typed nodes, catalogs, root-local order, four rotations, canonical artifact, and fail-closed diagnostics.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }
}
