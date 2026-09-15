namespace Komissar.Diagnostics.Metrics;

public sealed record SystemMetrics(string SystemName, long Tick, TimeSpan Duration, int Wave);