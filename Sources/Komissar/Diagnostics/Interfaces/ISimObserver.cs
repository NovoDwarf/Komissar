using Komissar.Diagnostics.Metrics;

namespace Komissar.Diagnostics.Interfaces;

public interface ISimObserver<TState>
{
    public void OnSystemCompleted(SystemMetrics metrics);
    public void OnTickCompleted(TickMetrics metrics);
}
