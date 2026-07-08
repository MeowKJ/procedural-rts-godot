using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer
{
    private BattleCursorState? _activeCursorState;
    private readonly Dictionary<string, Texture2D?> _cursorTextureCache = new();

    private void ApplyCommandCursor(CommandPreviewState preview)
    {
        var state = BattleCursorCatalog.StateForPreview(preview);
        if (state == _activeCursorState)
        {
            return;
        }

        _activeCursorState = state;
        var definition = BattleCursorCatalog.DefinitionFor(state);
        var shape = BattleCursorGodotShapes.ToInputShape(definition.Shape);
        var texture = LoadCursorTexture(definition.TexturePath);
        if (texture is not null)
        {
            Input.SetCustomMouseCursor(texture, shape, new Vector2(definition.HotspotX, definition.HotspotY));
            return;
        }

        Input.SetCustomMouseCursor(null, shape);
        Input.SetDefaultCursorShape(shape);
    }

    private Texture2D? LoadCursorTexture(string? texturePath)
    {
        if (string.IsNullOrWhiteSpace(texturePath))
        {
            return null;
        }

        if (_cursorTextureCache.TryGetValue(texturePath, out var cached))
        {
            return cached;
        }

        Texture2D? texture = null;
        using var image = new Image();
        var absolutePath = ProjectSettings.GlobalizePath(texturePath);
        if (image.Load(absolutePath) == Error.Ok)
        {
            texture = ImageTexture.CreateFromImage(image);
        }
        else if (ResourceLoader.Exists(texturePath))
        {
            texture = ResourceLoader.Load<Texture2D>(texturePath);
        }

        _cursorTextureCache[texturePath] = texture;
        return texture;
    }
}
