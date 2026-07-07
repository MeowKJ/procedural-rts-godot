using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts.Controllers;

public partial class SelectionController
{
    private AbilityKind? _armedAbility;

    public void ArmAbilityCommand(AbilityKind ability)
    {
        _rallyCommandArmed = false;
        _repairCommandArmed = false;
        if (!UseUnitBattlefieldInput() || !SelectionHasAbility(ability))
        {
            _armedAbility = null;
            StatusChanged?.Invoke(GameText.Format("ui.ability.unavailable", AbilityLabel(ability)));
            AudioCueRequested?.Invoke(TacticalAudioCue.Invalid);
            return;
        }

        if (ability == AbilityKind.Deploy)
        {
            FinishSelfAbilityCommand(ability);
            return;
        }

        _armedAbility = ability;
        StatusChanged?.Invoke(GameText.Format("ui.ability.armed", AbilityLabel(ability)));
        AudioCueRequested?.Invoke(TacticalAudioCue.Move);
        QueueRedraw();
    }

    private bool HandleArmedAbilityMouse(InputEventMouseButton mouse)
    {
        if (_armedAbility is not { } ability)
        {
            return false;
        }

        if (mouse.ButtonIndex == MouseButton.Left)
        {
            if (!mouse.Pressed)
            {
                FinishArmedAbilityCommand(ability, mouse.Position);
            }

            GetViewport().SetInputAsHandled();
            return true;
        }

        if (mouse.ButtonIndex == MouseButton.Right && mouse.Pressed)
        {
            CancelArmedAbilityCommand();
            GetViewport().SetInputAsHandled();
            return true;
        }

        return false;
    }

    private bool HandleArmedAbilityKey(InputEventKey key)
    {
        if (_armedAbility is null || key.Keycode != Key.Escape)
        {
            return false;
        }

        CancelArmedAbilityCommand();
        return true;
    }

    private CommandPreviewState ArmedAbilityPreview(Vector2 screenPosition, Vector2 worldPosition)
    {
        if (_armedAbility is not { } ability)
        {
            return CommandPreviewState.None;
        }

        var target = _hoveredUnitInstance?.Position
            ?? _hoveredBuildingProjection?.Position
            ?? worldPosition;
        return new CommandPreviewState(AbilityPreviewKind(ability), AbilityLabel(ability), screenPosition, target, true);
    }

    private void FinishSelfAbilityCommand(AbilityKind ability)
    {
        _lastGatewayRejectedStatus = string.Empty;
        var subjects = SelectedRuntimeUnitSubjects();
        var accepted = SubmitRuntimeCommand(PlayerCommandKind.Ability, PlayerCommandPayload.ForAbility(subjects, ability));
        StatusChanged?.Invoke(accepted
            ? GameText.Format("ui.ability.fired", AbilityLabel(ability))
            : GatewayRejectedStatus(GameText.Format("ui.ability.unavailable", AbilityLabel(ability))));
        AcknowledgeCommand(
            accepted ? CommandAcknowledgementKind.Move : CommandAcknowledgementKind.Invalid,
            AbilityAcknowledgePosition(subjects),
            accepted ? CommandAcknowledgementAudioCue.Move : CommandAcknowledgementAudioCue.Invalid);
        _armedAbility = null;
        ClearDrag();
    }

    private void FinishArmedAbilityCommand(AbilityKind ability, Vector2 screenPoint)
    {
        var worldPoint = ScreenToWorld(screenPoint);
        var payload = AbilityTargetPayload(ability, worldPoint);
        _lastGatewayRejectedStatus = string.Empty;
        var accepted = SubmitRuntimeCommand(PlayerCommandKind.Ability, payload);
        StatusChanged?.Invoke(accepted
            ? GameText.Format("ui.ability.fired", AbilityLabel(ability))
            : GatewayRejectedStatus(GameText.Format("ui.ability.unavailable", AbilityLabel(ability))));
        AcknowledgeCommand(
            accepted ? AbilityAcknowledgementKind(ability) : CommandAcknowledgementKind.Invalid,
            AbilityAcknowledgePosition(payload, worldPoint),
            accepted ? CommandAcknowledgementAudioCue.Move : CommandAcknowledgementAudioCue.Invalid);
        _armedAbility = null;
        ClearDrag();
    }

    private PlayerCommandPayload AbilityTargetPayload(AbilityKind ability, Vector2 worldPoint)
    {
        var subjects = SelectedRuntimeUnitSubjects();
        if (ability is AbilityKind.RepairField or AbilityKind.ShieldField)
        {
            if (_hoveredUnitInstance is { } unit
                && UnitBattlefield!.Relations.Relation(LocalPlayerSlotId, unit.PlayerSlotId) is PlayerRelation.Self or PlayerRelation.Allied)
            {
                return PlayerCommandPayload.ForAbilityEntityTarget(subjects, ability, unit.EntityId);
            }

            if (_hoveredBuildingProjection is { Relation: PlayerRelation.Self or PlayerRelation.Allied } building
                && UnitBattlefield!.BuildingEntityIdByTargetId(building.Id) is { } buildingEntity)
            {
                return PlayerCommandPayload.ForAbilityEntityTarget(subjects, ability, buildingEntity);
            }
        }

        return PlayerCommandPayload.ForAbilityPoint(subjects, ability, worldPoint.X, worldPoint.Y);
    }

    private Vector2 AbilityAcknowledgePosition(PlayerCommandPayload payload, Vector2 fallback)
    {
        return payload.HasTargetPoint ? new Vector2(payload.TargetPoint.X, payload.TargetPoint.Y) : fallback;
    }

    private Vector2 AbilityAcknowledgePosition(IReadOnlyList<EntityId> subjects)
    {
        foreach (var unit in UnitBattlefield!.Units)
        {
            if (unit.PlayerSlotId == LocalPlayerSlotId
                && unit.Selected
                && ContainsEntityId(subjects, unit.EntityId))
            {
                return unit.Position;
            }
        }

        return GetGlobalMousePosition();
    }

    private bool SelectionHasAbility(AbilityKind ability)
    {
        foreach (var unit in UnitBattlefield!.Units)
        {
            if (unit.PlayerSlotId == LocalPlayerSlotId
                && unit.Selected
                && unit.Hp > 0
                && unit.Spec.HasAbility(ability))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsEntityId(IReadOnlyList<EntityId> entityIds, EntityId candidate)
    {
        for (var index = 0; index < entityIds.Count; index++)
        {
            if (entityIds[index] == candidate)
            {
                return true;
            }
        }

        return false;
    }

    private void CancelArmedAbilityCommand()
    {
        _armedAbility = null;
        ClearDrag();
        StatusChanged?.Invoke(GameText.T("ui.status.ready"));
        QueueRedraw();
    }

    private static CommandPreviewKind AbilityPreviewKind(AbilityKind ability)
    {
        return ability == AbilityKind.RepairField ? CommandPreviewKind.Repair : CommandPreviewKind.TargetHover;
    }

    private static CommandAcknowledgementKind AbilityAcknowledgementKind(AbilityKind ability)
    {
        return ability == AbilityKind.RepairField ? CommandAcknowledgementKind.Repair : CommandAcknowledgementKind.Move;
    }

    private static string AbilityLabel(AbilityKind ability)
    {
        return ability switch
        {
            AbilityKind.RepairField => GameText.T("ui.ability.repairField"),
            AbilityKind.ShieldField => GameText.T("ui.ability.shieldField"),
            AbilityKind.Scan => GameText.T("ui.ability.scan"),
            AbilityKind.Deploy => GameText.T("ui.ability.deploy"),
            _ => ability.ToString(),
        };
    }
}
