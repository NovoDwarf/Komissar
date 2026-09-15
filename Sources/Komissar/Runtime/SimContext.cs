using Komissar.Systems;

namespace Komissar.Runtime;

public sealed class SimContext<TState>
{
    public SimContext(TState state, long tick, SimClock simClock, TimeSpan delta, DateTimeOffset timestamp, CommandBuffer<TState> commands)
    {
        State = state;
        Tick = tick;
        SimClock = simClock;
        Delta = delta;
        Timestamp = timestamp;
        Commands = commands;
    }

    public TState State { get; }

    public long Tick { get; }

    public SimClock SimClock { get; }
    public TimeSpan Delta { get; }
    public DateTimeOffset Timestamp { get; }
    public CommandBuffer<TState> Commands { get; }
}
