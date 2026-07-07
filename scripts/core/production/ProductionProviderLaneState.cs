namespace ProceduralRts.Core;

public enum ProductionProviderLaneScope
{
    Auto,
    All,
    Specific
}

public sealed record ProductionProviderLaneState(
    ProductionProviderLaneScope Scope,
    int ProducerId,
    string ProducerKind,
    string Label,
    string ShortLabel,
    int ProviderCount,
    int QueueCount,
    float ActiveProgress,
    bool Available,
    string DisabledReasonKey,
    string? RepeatOutputSpecId = null);
