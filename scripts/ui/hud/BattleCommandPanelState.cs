using System;
using System.Collections.Generic;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public enum BattleCommandPanelMode
{
    Build,
    Train,
    Upgrades,
    Abilities,
}

public sealed record BattleCommandPanelState(
    BattleCommandPanelMode Mode,
    IReadOnlyList<BuildOptionSnapshot> BuildCards,
    IReadOnlyList<BuildOptionSnapshot> VisibleBuildCards,
    IReadOnlyList<ProductionProviderLaneState> ConstructionProviderLanes,
    ProductionProviderLaneScope SelectedConstructionProviderScope,
    int SelectedConstructionProviderId,
    IReadOnlyList<ProductionOptionState> TrainCards,
    IReadOnlyList<ProductionOptionState> VisibleTrainCards,
    IReadOnlyList<ProductionProviderLaneState> ProductionProviderLanes,
    ProductionProviderLaneScope SelectedProductionProviderScope,
    int SelectedProductionProviderId)
{
    public int StartableBuildCount => CountStartableBuildCards(VisibleBuildCards);
    public int QueueableTrainCount => CountQueueableTrainCards(VisibleTrainCards);

    public static BattleCommandPanelState Empty { get; } = new(
        BattleCommandPanelMode.Train,
        Array.Empty<BuildOptionSnapshot>(),
        Array.Empty<BuildOptionSnapshot>(),
        Array.Empty<ProductionProviderLaneState>(),
        ProductionProviderLaneScope.Auto,
        0,
        Array.Empty<ProductionOptionState>(),
        Array.Empty<ProductionOptionState>(),
        Array.Empty<ProductionProviderLaneState>(),
        ProductionProviderLaneScope.Auto,
        0);

    private static int CountStartableBuildCards(IReadOnlyList<BuildOptionSnapshot> cards)
    {
        var count = 0;
        for (var index = 0; index < cards.Count; index++)
        {
            if (cards[index].CanStart)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountQueueableTrainCards(IReadOnlyList<ProductionOptionState> cards)
    {
        var count = 0;
        for (var index = 0; index < cards.Count; index++)
        {
            if (cards[index].CanQueue)
            {
                count++;
            }
        }

        return count;
    }
}
