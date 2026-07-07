using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts.Controllers;

public partial class SelectionController
{
    private CommandPreviewState CreatePreviewState(Vector2 screenPosition, Vector2 worldPosition)
    {
        if (_dragStartScreen is not null)
        {
            return new CommandPreviewState(CommandPreviewKind.Select, GameText.T("preview.select"), screenPosition, worldPosition, true);
        }

        if (_rallyCommandArmed)
        {
            return ArmedRallyPreview(screenPosition, worldPosition);
        }

        if (_armedAbility is not null)
        {
            return ArmedAbilityPreview(screenPosition, worldPosition);
        }

        if (_hoveredUnitInstance is { } hoveredUnitInstance)
        {
            var relation = UnitBattlefield!.Relations.Relation(LocalPlayerSlotId, hoveredUnitInstance.PlayerSlotId);
            if (relation == PlayerRelation.Hostile && UnitBattlefield.SelectedCount(LocalPlayerSlotId) > 0)
            {
                return new CommandPreviewState(CommandPreviewKind.Attack, RuntimeUnitAttackPreviewLabel(hoveredUnitInstance.Spec), screenPosition, hoveredUnitInstance.Position, true);
            }

            if (UnitBattlefield.CanRepairSelected(LocalPlayerSlotId, hoveredUnitInstance))
            {
                return new CommandPreviewState(CommandPreviewKind.Repair, GameText.T("ui.context.repair"), screenPosition, hoveredUnitInstance.Position, true);
            }

            return new CommandPreviewState(CommandPreviewKind.TargetHover, relation == PlayerRelation.Hostile ? GameText.T("preview.enemy") : GameText.T("preview.unit"), screenPosition, hoveredUnitInstance.Position, true);
        }

        if (_hoveredUnit is { } hoveredUnit)
        {
            var isEnemy = State.IsHostileToPlayer(hoveredUnit);
            if (isEnemy && HasSelectedLegacyUnits())
            {
                return new CommandPreviewState(CommandPreviewKind.Attack, LegacyUnitAttackPreviewLabel(hoveredUnit), screenPosition, hoveredUnit.Position, true);
            }

            return new CommandPreviewState(CommandPreviewKind.TargetHover, isEnemy ? GameText.T("preview.enemy") : GameText.T("preview.unit"), screenPosition, hoveredUnit.Position, true);
        }

        if (_hoveredBuildingProjection is { } buildingProjection)
        {
            var isEnemy = buildingProjection.Relation == PlayerRelation.Hostile;
            if (isEnemy && UnitBattlefield!.SelectedCount(LocalPlayerSlotId) > 0)
            {
                return new CommandPreviewState(CommandPreviewKind.Attack, RuntimeBuildingAttackPreviewLabel(buildingProjection), screenPosition, buildingProjection.Position, true);
            }

            if (UnitBattlefield!.CanRepairSelectedBuilding(LocalPlayerSlotId, buildingProjection.Id))
            {
                return new CommandPreviewState(CommandPreviewKind.Repair, GameText.T("ui.context.repair"), screenPosition, buildingProjection.Position, true);
            }

            return new CommandPreviewState(CommandPreviewKind.TargetHover, isEnemy ? GameText.T("preview.enemyStructure") : GameText.T("preview.structure"), screenPosition, buildingProjection.Position, true);
        }

        if (_hoveredBuilding is { } hoveredBuilding)
        {
            var isEnemy = State.IsHostileToPlayer(hoveredBuilding);
            if (isEnemy && HasSelectedLegacyUnits())
            {
                return new CommandPreviewState(CommandPreviewKind.Attack, LegacyBuildingAttackPreviewLabel(hoveredBuilding), screenPosition, hoveredBuilding.Position, true);
            }

            return new CommandPreviewState(CommandPreviewKind.TargetHover, isEnemy ? GameText.T("preview.enemyStructure") : GameText.T("preview.structure"), screenPosition, hoveredBuilding.Position, true);
        }

        if (_hoveredResourceField is { } resourceField)
        {
            var hasHarvester = HasSelectedHarvester();
            if (hasHarvester)
            {
                return new CommandPreviewState(CommandPreviewKind.Harvest, GameText.T("preview.harvest"), screenPosition, resourceField.Position, true);
            }

            if (HasSelectedBuildingForPreview())
            {
                return new CommandPreviewState(CommandPreviewKind.Rally, GameText.T("preview.setRally"), screenPosition, resourceField.Position, true);
            }

            return new CommandPreviewState(CommandPreviewKind.TargetHover, GameText.T("preview.resource"), screenPosition, resourceField.Position, true);
        }

        if (UseUnitBattlefieldInput() && UnitBattlefield!.SelectedCount(LocalPlayerSlotId) > 0)
        {
            return new CommandPreviewState(CommandPreviewKind.Move, GameText.T("preview.move"), screenPosition, worldPosition, true);
        }

        if (HasSelectedLegacyUnits())
        {
            return new CommandPreviewState(CommandPreviewKind.Move, GameText.T("preview.move"), screenPosition, worldPosition, true);
        }

        if (HasSelectedBuildingForPreview())
        {
            return new CommandPreviewState(CommandPreviewKind.Rally, GameText.T("preview.setRally"), screenPosition, worldPosition, true);
        }

        return new CommandPreviewState(CommandPreviewKind.Select, GameText.T("preview.select"), screenPosition, worldPosition, true);
    }
}
