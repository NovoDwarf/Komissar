using Komissar.Core.Enums;

namespace Komissar.Utilities;

public static class SimpleValidator
{
	public static void EnsureCanStart(SimStatus status)
	{
		switch (status)
		{
			case SimStatus.Created:
			case SimStatus.Paused: return;
			case SimStatus.Running: throw new InvalidOperationException("The simulation is already running.");
			case SimStatus.Stopped: throw new InvalidOperationException("A stopped simulation cannot be started.");
			default: throw new InvalidOperationException($"Unsupported simulation status: {status}.");
		}
	}

	public static void EnsureCanStep(SimStatus status)
	{
		if (status == SimStatus.Stopped)
			throw new InvalidOperationException("A stopped simulation cannot execute a step.");
	}
}