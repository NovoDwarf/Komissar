using System.Diagnostics.Metrics;
using Komissar.Diagnostics.Interfaces;
using Komissar.Diagnostics.Metrics;

namespace Komissar.Diagnostics;

public sealed class MetricsCollector<TState> : ISimObserver<TState>, IDisposable
{
    private readonly Lock _sync = new();
    private readonly Meter _meter = new("Komissar.Simulation");
  
    private readonly Histogram<double> _tickDuration;
    private readonly Histogram<double> _systemDuration;
    
    public MetricsCollector()
    {
        _tickDuration = _meter.CreateHistogram<double>("komissar.simulation.tick.duration", "ms");
        _systemDuration = _meter.CreateHistogram<double>("komissar.simulation.system.duration", "ms");
        
        _meter.CreateObservableCounter("komissar.simulation.ticks", () => Interlocked.Read(ref _completedTicks));
    }
    
    private TickMetrics? _lastTick;
    private long _completedTicks;

    public TickMetrics? LastTick
    {
        get
        {
            lock (_sync)
                return _lastTick;
        }
    }

    public void OnSystemCompleted(SystemMetrics metrics)
    {
        _systemDuration.Record(metrics.Duration.TotalMilliseconds,
            new KeyValuePair<string, object?>("system", metrics.SystemName),
            new KeyValuePair<string, object?>("wave", metrics.Wave));
    }

    public void OnTickCompleted(TickMetrics metrics)
    {
        lock (_sync)
            _lastTick = metrics;

        Interlocked.Increment(ref _completedTicks);
        
        _tickDuration.Record(metrics.Duration.TotalMilliseconds,
            new KeyValuePair<string, object?>("systems", metrics.SystemCount),
            new KeyValuePair<string, object?>("waves", metrics.WaveCount));
    }

    public void Dispose() => _meter.Dispose();
}
