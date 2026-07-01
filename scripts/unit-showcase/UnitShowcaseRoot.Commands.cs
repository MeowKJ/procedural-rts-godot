using Godot;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ProceduralRts;

public partial class UnitShowcaseRoot
{
    private void HandleUnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        {
            GetTree().Quit();
        }
    }

    private void SaveCapture()
    {
        var image = GetViewport().GetTexture().GetImage();
        var absolutePath = ProjectSettings.GlobalizePath($"res://{CapturePath}");
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        var error = image.SavePng(absolutePath);
        if (error != Error.Ok)
        {
            throw new InvalidOperationException($"Failed to save unit showcase screenshot: {error}");
        }

        GD.Print($"Unit showcase screenshot saved to {absolutePath}");
    }

    private async Task NextFrames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }
}
