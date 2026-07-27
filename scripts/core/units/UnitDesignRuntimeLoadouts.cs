using Godot;

namespace ProceduralRts.Core;

public readonly record struct UnitDesignSpawn(string DesignId, Vector2 Offset, float FacingOffset = 0);

public static class UnitDesignRuntimeLoadouts
{
    public static IReadOnlyList<UnitDesignSpawn> StartingUnits(UnitFactionId faction)
    {
        return UnitDesignFactionRosterCatalog.StartingUnits(faction);
    }

    public static string? ProductionDesignId(UnitFactionId faction, ProductionSpec production)
    {
        return UnitDesignFactionRosterCatalog.ProductionDesignId(faction, production);
    }

}
