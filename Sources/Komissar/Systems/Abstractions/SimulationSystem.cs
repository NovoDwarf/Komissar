using Komissar.Core.Enums;
using Komissar.Runtime;
using Komissar.Systems.Interfaces;

namespace Komissar.Systems.Abstractions;

public abstract class SimulationSystem<TState> : ISimulationSystem<TState>
{
	public abstract void Execute(SimContext<TState> context);

	public abstract SimExecMode ExecutionMode { get; }
    
	public ValueTask ExecuteAsync(SimContext<TState> context, CancellationToken cancellationToken)
	{
		Execute(context);
		return ValueTask.CompletedTask;
	}
}