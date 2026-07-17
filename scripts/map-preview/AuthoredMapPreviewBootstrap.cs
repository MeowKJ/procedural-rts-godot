using Godot;
using ProceduralRts.Core;

namespace ProceduralRts;

public partial class AuthoredMapPreviewBootstrap : Node
{
    private string _stagedMapId = "";
    private string _stagedHash = "";

    public override void _Ready()
    {
        var userArguments = OS.GetCmdlineUserArgs();
        try
        {
            var request = AuthoredMapPreviewRequest.Parse(userArguments);
            var projectRoot = Path.GetFullPath(ProjectSettings.GlobalizePath("res://"));
            var map = AuthoredMapPreviewRuntime.StageVerified(request, projectRoot);
            _stagedMapId = map.Id;
            _stagedHash = request.Sha256;
            CallDeferred(nameof(LaunchBattle));
        }
        catch (Exception exception)
        {
            Fail(exception);
        }
    }


    private void LaunchBattle()
    {
        try
        {
            GD.Print($"Authored map preview staged: id={_stagedMapId} sha256={_stagedHash}");
            if (OS.GetEnvironment("MAP_AUTHORING_BAKE_PLAY_SMOKE") == "1")
                GetTree().Root.AddChild(new AuthoredMapPreviewRuntimeSmoke { Name = "AuthoredMapPreviewRuntimeSmoke" });
            var error = GetTree().ChangeSceneToFile("res://scenes/Battle.tscn");
            if (error != Error.Ok) throw new InvalidOperationException($"Battle scene load failed: {error}.");
        }
        catch (Exception exception)
        {
            Fail(exception);
        }
    }

    private void Fail(Exception exception)
    {
        SkirmishSetupState.ClearAuthoredMapHandoff();
        var message = exception.Message.Length <= 240 ? exception.Message : exception.Message[..240];
        GD.PushError($"Authored preview bootstrap rejected: {message}");
        GetTree().Quit(2);
    }
}
