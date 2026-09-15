using Komissar.Core.Enums;
using Komissar.Runtime;

namespace Komissar.Systems.Interfaces;

public interface ISimulationSystem<TState>
{
    public SimExecMode ExecutionMode { get; }

    public void Execute(SimContext<TState> context);
    
    public ValueTask ExecuteAsync(SimContext<TState> context, CancellationToken cancellationToken);
}