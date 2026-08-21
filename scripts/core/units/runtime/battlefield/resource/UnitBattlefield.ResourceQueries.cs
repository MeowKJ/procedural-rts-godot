using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public ResourceInventory ResourceInventory(PlayerSlotId playerSlotId)
    {
        return _entityWorld.ResourceInventory(OwnerId.FromPlayerSlot(playerSlotId));
    }

    public int Credits(PlayerSlotId playerSlotId)
    {
        return ResourceInventory(playerSlotId).Credits;
    }

    public void SetCredits(PlayerSlotId playerSlotId, int credits)
    {
        var inventory = ResourceInventory(playerSlotId);
        inventory.Credits = Mathf.Max(0, credits);
        ResourceInventoryChanged?.Invoke(playerSlotId, inventory);
    }

    public UnitBattlefieldResourceNodeProjection? PickResourceNode(Vector2 worldPoint, float pickPadding = 8)
    {
        return NearestResourceNode(worldPoint, pickPadding);
    }

    public UnitBattlefieldResourceNodeProjection? NearestVisibleResourceNode(OwnerId owner, Vector2 origin)
    {
        UnitBattlefieldResourceNodeProjection? best = null;
        var bestDistance = float.PositiveInfinity;
        foreach (var resource in ResourceNodeProjections())
        {
            if (resource.Amount <= 0
                || !EntityWorld.Visibility.IsVisible(owner, resource.EntityId))
            {
                continue;
            }

            var distance = resource.Position.DistanceSquaredTo(origin);
            if (distance < bestDistance)
            {
                best = resource;
                bestDistance = distance;
            }
        }

        return best;
    }

    public IReadOnlyList<UnitBattlefieldResourcePip> ResourcePips(Func<Vector2, bool>? isExplored = null)
    {
        var result = NextResourcePipBuffer();
        foreach (var field in ResourceNodeProjections())
        {
            if (field.Amount <= 0 || !(isExplored?.Invoke(field.Position) ?? true))
            {
                continue;
            }

            result.Add(new UnitBattlefieldResourcePip(
                field.Position,
                field.Radius,
                field.MaxAmount <= 0 ? 0 : Mathf.Clamp((float)field.Amount / field.MaxAmount, 0, 1)));
        }

        return result;
    }

    private List<UnitBattlefieldResourcePip> NextResourcePipBuffer()
    {
        _useSecondaryResourcePipBuffer = !_useSecondaryResourcePipBuffer;
        var result = _useSecondaryResourcePipBuffer ? _resourcePipSecondaryBuffer : _resourcePipBuffer;
        result.Clear();
        return result;
    }
}
