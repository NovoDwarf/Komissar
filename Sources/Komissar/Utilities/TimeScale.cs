namespace Komissar.Utilities;

public sealed class TimeScale
{
    public double Speed
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);

            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), "Speed must be a finite number.");

            field = value;
        }
    } = 60.0;

    public bool IsPaused { get; set; }

    public TimeSpan Convert(TimeSpan realDelta)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(realDelta, TimeSpan.Zero);

        if (IsPaused || Speed == 0.0)
            return TimeSpan.Zero;

        if (Speed == 1.0)
            return realDelta;

        var scaledTicks = realDelta.Ticks * Speed;

        if (scaledTicks > TimeSpan.MaxValue.Ticks)
            throw new OverflowException("Scaled simulation time exceeds TimeSpan.MaxValue.");

        return TimeSpan.FromTicks(checked((long)scaledTicks));
    }
}
