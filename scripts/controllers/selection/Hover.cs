using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Controllers;

public partial class SelectionController
{
    private void DrawHoverAffordance()
    {
        if (_hoveredUnitInstance is not null)
        {
            DrawUnitInstanceHoverAffordance(_hoveredUnitInstance);
            return;
        }

        if (_hoveredBuildingProjection is null && _hoveredResourceNode is null && _hoveredUnitInstance is null)
        {
            return;
        }

        if (_hoveredBuildingProjection is { } buildingProjection)
        {
            DrawUnitBattlefieldBuildingHoverAffordance(buildingProjection);
            return;
        }

        if (_hoveredResourceNode is { } resourceNode)
        {
            DrawResourceHoverAffordance(resourceNode);
            return;
        }

    }

    private void DrawUnitInstanceHoverAffordance(UnitInstance unit)
    {
        var relation = UnitBattlefield!.Relations.Relation(LocalPlayerSlotId, unit.PlayerSlotId);
        var color = UnitRelationAccent(relation);
        var isEnemy = relation == PlayerRelation.Hostile;
        var radius = unit.Spec.Collision.Radius + (isEnemy ? 18 : 13);
        var center = unit.Position;
        var bracket = Mathf.Max(8, radius * 0.34f);

        DrawArc(center, radius, 0, Mathf.Tau, 96, new Color(color, isEnemy ? 0.34f : 0.24f), 1.2f, true);
        DrawLine(center + new Vector2(-radius, -bracket), center + new Vector2(-radius, bracket), color, 2.2f, true);
        DrawLine(center + new Vector2(radius, -bracket), center + new Vector2(radius, bracket), color, 2.2f, true);
        DrawLine(center + new Vector2(-bracket, -radius), center + new Vector2(bracket, -radius), color, 2.2f, true);
        DrawLine(center + new Vector2(-bracket, radius), center + new Vector2(bracket, radius), color, 2.2f, true);
    }

    private void DrawUnitBattlefieldBuildingHoverAffordance(BuildingHoverProjection projection)
    {
        var relation = projection.Relation;
        var color = UnitRelationAccent(relation);
        var isEnemy = relation == PlayerRelation.Hostile;
        var radius = projection.Radius + (isEnemy ? 18 : 12);
        var center = projection.Position;
        var bracket = Mathf.Max(14, radius * 0.26f);

        DrawArc(center, radius, 0, Mathf.Tau, 112, new Color(color, isEnemy ? 0.3f : 0.2f), 1.4f, true);
        DrawLine(center + new Vector2(-radius, -bracket), center + new Vector2(-radius, bracket), color, 2.2f, true);
        DrawLine(center + new Vector2(radius, -bracket), center + new Vector2(radius, bracket), color, 2.2f, true);
        DrawLine(center + new Vector2(-bracket, -radius), center + new Vector2(bracket, -radius), color, 2.2f, true);
        DrawLine(center + new Vector2(-bracket, radius), center + new Vector2(bracket, radius), color, 2.2f, true);
    }

    private void DrawResourceHoverAffordance(UnitBattlefieldResourceNodeProjection resource)
    {
        var fullness = resource.MaxAmount <= 0 ? 0 : Mathf.Clamp((float)resource.Amount / resource.MaxAmount, 0, 1);
        var color = new Color(resource.Accent, 0.72f);
        var radius = resource.Radius + 22;
        var center = resource.Position;
        DrawArc(center, radius, 0, Mathf.Tau, 128, new Color(resource.Accent, 0.24f + fullness * 0.22f), 2.2f, true);
        DrawArc(center, radius + 12, -Mathf.Pi / 2f, -Mathf.Pi / 2f + Mathf.Tau * fullness, 96, new Color("#ffffff", 0.52f), 2.4f, true);

        if (!HasSelectedHarvester())
        {
            return;
        }

        DrawLine(center + new Vector2(-12, 0), center + new Vector2(12, 0), color, 2.2f, true);
        DrawLine(center + new Vector2(0, -12), center + new Vector2(0, 12), color, 2.2f, true);
    }
}
