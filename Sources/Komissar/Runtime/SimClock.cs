namespace Komissar.Runtime;

public sealed class SimClock
{
    public TimeSpan Elapsed { get; private set; }

    public void Advance(TimeSpan delta)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delta, TimeSpan.Zero);

        Elapsed += delta;
    }
}
