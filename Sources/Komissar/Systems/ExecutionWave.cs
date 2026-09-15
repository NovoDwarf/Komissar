using Komissar.Core.Enums;
using Komissar.Systems.Interfaces;

namespace Komissar.Systems;

public class ExecutionWave<TState>
{
	public ExecutionWave(
		int index,
		IReadOnlyList<ISimulationSystem<TState>> systems,
		SimExecMode executionMode)
	{
		Index = index;
		Systems = systems;
		ExecutionMode = executionMode;
	}
	
	public int Index { get; }
	public IReadOnlyList<ISimulationSystem<TState>> Systems { get; }
	public SimExecMode ExecutionMode { get; }
}