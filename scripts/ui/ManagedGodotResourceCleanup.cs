using System.Reflection;
using Godot;

namespace ProceduralRts.Ui;

public static class ManagedGodotResourceCleanup
{
    private static readonly string[] StyleboxOverrideNames =
    [
        "panel",
        "normal",
        "hover",
        "pressed",
        "disabled",
        "focus",
    ];

    public static void ReleaseTree(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            ReleaseTree(child);
        }

        ReleaseNode(node);
    }

    private static void ReleaseNode(Node node)
    {
        ReleaseTextureProperties(node);

        if (node is Label label && label.LabelSettings is { } settings)
        {
            label.LabelSettings = null;
            DisposeGodotObject(settings);
        }

        if (node is Control control)
        {
            foreach (var name in StyleboxOverrideNames)
            {
                if (!control.HasThemeStyleboxOverride(name))
                {
                    continue;
                }

                var style = control.GetThemeStylebox(name);
                control.RemoveThemeStyleboxOverride(name);
                DisposeGodotObject(style);
            }
        }

        if (node is AudioStreamPlayer player)
        {
            player.Stop();
            player.Stream = null;
        }

        if (node is CanvasItem canvas && canvas.Material is { } material)
        {
            canvas.Material = null;
            if (material is ShaderMaterial shaderMaterial && shaderMaterial.Shader is { } shader)
            {
                shaderMaterial.Shader = null;
                DisposeGodotObject(shader);
            }

            DisposeGodotObject(material);
        }
    }

    private static void ReleaseTextureProperties(Node node)
    {
        var type = node.GetType();
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!property.CanRead || !property.CanWrite || !typeof(Texture2D).IsAssignableFrom(property.PropertyType))
            {
                continue;
            }

            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            try
            {
                property.SetValue(node, null);
            }
            catch (TargetInvocationException)
            {
            }
            catch (ArgumentException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }
    }

    public static void DisposeGodotObject(GodotObject? value)
    {
        if (value is null)
        {
            return;
        }

        try
        {
            value.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }
}
