namespace Komissar.Systems;

public readonly record struct SimExecContext(long Tick, TimeSpan Delta, DateTimeOffset Timestamp);