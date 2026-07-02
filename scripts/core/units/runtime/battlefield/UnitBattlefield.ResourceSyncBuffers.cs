namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private bool HasHarvesters()
    {
        foreach (var unit in Units)
        {
            if (IsHarvester(unit))
            {
                return true;
            }
        }

        return false;
    }

    private void CollectResourceCreditsBefore(Dictionary<PlayerSlotId, int> result)
    {
        result.Clear();
        foreach (var pair in ResourceInventories)
        {
            result[pair.Key] = pair.Value.Credits;
        }
    }
}
