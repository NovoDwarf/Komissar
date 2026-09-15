using Komissar.Core.Enums;
using Komissar.Utilities;

namespace Komissar.Runtime.Interfaces;

public interface ISim<TState> : IDisposable
{
    public SimMode Mode { get; }
    public SimStatus Status { get; }

    public long Tick { get; }

    public SimClock SimClock { get; }
    public TimeScale TimeScale { get; }

    public void Start();

    public void Pause();

    public void Stop();

    public void Step();

    public void Step(TimeSpan delta);

    public void Update(TimeSpan realDelta);
}