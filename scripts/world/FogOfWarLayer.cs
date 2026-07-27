using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts.World;

public partial class FogOfWarLayer : Node2D
{
    private const string FogShaderCode = """
        shader_type canvas_item;

        uniform vec4 unexplored_color : source_color = vec4(0.0, 0.0, 0.0, 0.97);
        uniform vec4 explored_color : source_color = vec4(0.008, 0.027, 0.050, 0.54);
        uniform vec2 visibility_smoothstep = vec2(0.35, 0.75);

        void fragment() {
            vec4 mask = texture(TEXTURE, UV);
            float visible = smoothstep(visibility_smoothstep.x, visibility_smoothstep.y, mask.r);
            float explored = smoothstep(visibility_smoothstep.x, visibility_smoothstep.y, mask.g);
            vec4 fog = mix(unexplored_color, explored_color, explored);
            fog.a *= 1.0 - visible;
            COLOR = fog;
        }
        """;

    private float _redrawTimer;
    private int _lastDrawnMaskRevision = -1;
    private Rect2? _lastTextureRequestRect;

    public required FogOfWarMap FogOfWar { get; init; }
    public required Vector2 WorldSize { get; init; }
    public FogQualityTier Quality { get; init; } = FogQualityTier.Medium;
    public Rect2? VisibleWorldRect { get; set; }

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Linear;
        var material = new ShaderMaterial
        {
            Shader = new Shader
            {
                Code = FogShaderCode,
            },
        };
        material.SetShaderParameter("unexplored_color", FogOfWarVisualPolicy.UnexploredOverlay);
        material.SetShaderParameter("explored_color", FogOfWarVisualPolicy.ExploredMemoryOverlay);
        material.SetShaderParameter(
            "visibility_smoothstep",
            new Vector2(FogOfWarVisualPolicy.ShaderVisibilityLow, FogOfWarVisualPolicy.ShaderVisibilityHigh));
        Material = material;
    }

    public override void _ExitTree()
    {
        if (Material is not { } material)
        {
            return;
        }

        Material = null;
        if (material is ShaderMaterial shaderMaterial && shaderMaterial.Shader is { } shader)
        {
            shaderMaterial.Shader = null;
            ManagedGodotResourceCleanup.DisposeGodotObject(shader);
        }

        ManagedGodotResourceCleanup.DisposeGodotObject(material);
    }

    public override void _Process(double delta)
    {
        _redrawTimer -= (float)delta;
        if (ShouldQueueImmediateFogUpload())
        {
            _redrawTimer = FogOfWarVisualPolicy.WorldRedrawIntervalFor(Quality);
            QueueRedraw();
            return;
        }

        if (_redrawTimer > 0)
        {
            return;
        }

        _redrawTimer = FogOfWarVisualPolicy.WorldRedrawIntervalFor(Quality);
        if (ShouldQueueFogRedraw())
        {
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        var texture = FogOfWar.MaskTexture(VisibleWorldRect);
        if (texture is null)
        {
            return;
        }

        DrawTextureRect(texture, new Rect2(Vector2.Zero, WorldSize), false);
        _lastDrawnMaskRevision = FogOfWar.MaskRevision;
        _lastTextureRequestRect = VisibleWorldRect ?? new Rect2(Vector2.Zero, WorldSize);
    }

    private bool ShouldQueueFogRedraw()
    {
        if (_lastTextureRequestRect is null)
        {
            return true;
        }

        if (!FogOfWar.HasPendingMaskTextureUpload(VisibleWorldRect))
        {
            return false;
        }

        if (FogOfWar.MaskRevision != _lastDrawnMaskRevision)
        {
            return true;
        }

        return CameraScopedRectMoved(_lastTextureRequestRect.Value, VisibleWorldRect);
    }

    private bool ShouldQueueImmediateFogUpload()
    {
        return FogOfWar.MaskRevision != _lastDrawnMaskRevision
            && FogOfWar.HasPendingMaskTextureUpload(VisibleWorldRect);
    }

    private bool CameraScopedRectMoved(Rect2 previous, Rect2? current)
    {
        if (current is not { } currentRect)
        {
            return false;
        }

        var step = FogOfWarVisualPolicy.CameraScopedUploadWorldStepFor(Quality);
        return previous.GetCenter().DistanceSquaredTo(currentRect.GetCenter()) >= step * step
            || Mathf.Abs(previous.Size.X - currentRect.Size.X) >= step
            || Mathf.Abs(previous.Size.Y - currentRect.Size.Y) >= step;
    }
}
