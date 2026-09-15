using Komissar.Diagnostics.Interfaces;
using Komissar.Systems;
using Komissar.Systems.Interfaces;
using Komissar.Utilities;

namespace Komissar.Runtime;

public sealed class SimSync<TState> : SimBase<TState>
{
    public SimSync(
        TState state,
        SimOptions options,
        SystemPlanner<TState> planner,
        IEnumerable<ISimulationSystem<TState>> systems,
        TimeProvider? timeProvider = null,
        SimClock? clock = null,
        TimeScale? timeScale = null,
        IEnumerable<ISimObserver<TState>>? observers = null)
        : base(
            state,
            options,
            planner,
            systems,
            timeProvider,
            clock,
            timeScale,
            observers)
    {
    }

    public override void Start()
    {
        ThrowIfDisposed();
        StartCore();
    }

    public override void Pause()
    {
        ThrowIfDisposed();
        PauseCore();
    }

    public override void Stop()
    {
        ThrowIfDisposed();
        StopCore();
    }

    public override void Step()
    {
        Step(Options.TickInterval);
    }

    public override void Step(TimeSpan delta)
    {
        ThrowIfDisposed();

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(delta, TimeSpan.Zero);

        EnsureCanExecuteStep();
        ExecuteStep(delta);
    }

    public override void Update(TimeSpan realDelta)
    {
        ThrowIfDisposed();
        
        if (!IsRunning())
            return;

        var simulationDelta = TimeScale.Convert(realDelta);

        ExecuteStep(simulationDelta);
    }

    public override void Dispose()
    {
        if (!TryMarkDisposed())
            return;

        StopCore();
    }

    private void ExecuteStep(TimeSpan delta)
    {
        var nextTick = Tick + 1;

        var timestamp = TimeProvider.GetUtcNow();
        var startedAt = TimeProvider.GetTimestamp();

        var executionContext = new SimExecContext(nextTick, delta, timestamp);

        Executor.Execute(ExecutionPlan, executionContext);

        CommitTick(nextTick, delta);

        var metrics = CreateTickMetrics(nextTick, delta, startedAt);

        NotifyTickCompleted(metrics);
    }
}