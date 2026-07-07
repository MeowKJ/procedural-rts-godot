using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts.Controllers;

public partial class SelectionController
{
    private bool _rallyCommandArmed;

    public void ArmRallyCommand()
    {
        _armedAbility = null;
        if (!HasSelectedBuildingForPreview())
        {
            _rallyCommandArmed = false;
            StatusChanged?.Invoke(GameText.T("rally.selectProducer"));
            AudioCueRequested?.Invoke(TacticalAudioCue.Invalid);
            return;
        }

        _rallyCommandArmed = true;
        StatusChanged?.Invoke(GameText.T("preview.setRally"));
        AudioCueRequested?.Invoke(TacticalAudioCue.Move);
        QueueRedraw();
    }

    private bool HandleArmedRallyMouse(InputEventMouseButton mouse)
    {
        if (!_rallyCommandArmed)
        {
            return false;
        }

        if (mouse.ButtonIndex == MouseButton.Left)
        {
            if (!mouse.Pressed)
            {
                FinishArmedRallyCommand(mouse.Position);
            }

            GetViewport().SetInputAsHandled();
            return true;
        }

        if (mouse.ButtonIndex == MouseButton.Right && mouse.Pressed)
        {
            CancelArmedRallyCommand();
            GetViewport().SetInputAsHandled();
            return true;
        }

        return false;
    }

    private bool HandleArmedRallyKey(InputEventKey key)
    {
        if (!_rallyCommandArmed || key.Keycode != Key.Escape)
        {
            return false;
        }

        CancelArmedRallyCommand();
        return true;
    }

    private CommandPreviewState ArmedRallyPreview(Vector2 screenPosition, Vector2 worldPosition)
    {
        var target = _hoveredResourceField?.Position ?? worldPosition;
        return new CommandPreviewState(CommandPreviewKind.Rally, GameText.T("preview.setRally"), screenPosition, target, true);
    }

    private void FinishArmedRallyCommand(Vector2 screenPoint)
    {
        var worldPoint = ScreenToWorld(screenPoint);
        if (UseUnitBattlefieldInput()
            && PickResourceField(worldPoint) is { } rallyResource
            && UnitBattlefield!.HasSelectedBuildings(LocalPlayerSlotId))
        {
            FinishSelectedBuildingRallyCommand(rallyResource);
        }
        else
        {
            FinishSelectedBuildingRallyCommand(worldPoint);
        }

        _rallyCommandArmed = false;
        ClearDrag();
    }

    private void CancelArmedRallyCommand()
    {
        _rallyCommandArmed = false;
        ClearDrag();
        StatusChanged?.Invoke(GameText.T("ui.status.ready"));
    }
}
