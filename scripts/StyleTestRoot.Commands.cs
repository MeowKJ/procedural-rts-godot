using Godot;
using System.IO;

namespace ProceduralRts;

public partial class StyleTestRoot
{
    private static bool ShouldCapture()
    {
        if (OS.GetEnvironment("STYLE_TEST_CAPTURE") == "1")
        {
            return true;
        }

        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg == CaptureArg)
            {
                return true;
            }
        }

        return false;
    }

    private void SaveCapture()
    {
        var image = GetViewport().GetTexture().GetImage();
        var absolutePath = ProjectSettings.GlobalizePath($"res://{CapturePath}");
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        var error = image.SavePng(absolutePath);
        if (error != Error.Ok)
        {
            throw new InvalidOperationException($"Failed to save style test screenshot: {error}");
        }

        GD.Print($"Style test screenshot saved to {absolutePath}");
    }

    private async Task NextFrames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }
}
