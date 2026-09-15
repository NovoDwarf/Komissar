using Komissar.Core.Enums;
using Komissar.Diagnostics.Interfaces;
using Komissar.Diagnostics.Metrics;
using Komissar.Runtime;
using Komissar.Systems.Interfaces;

namespace Komissar.Systems;

public sealed class SimExecutor<TState>
{
    private readonly TState _state;

    private readonly SimClock _simClock;
    private readonly SimOptions _options;
    private readonly TimeProvider _timeProvider;

    private readonly IReadOnlyList<ISimObserver<TState>> _observers;

    public SimExecutor(
        TState state,
        SimClock simClock,
        SimOptions options,
        TimeProvider timeProvider,
        IReadOnlyList<ISimObserver<TState>> observers)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(simClock);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(observers);

        _state = state;
        _simClock = simClock;
        _options = options;
        _timeProvider = timeProvider;
        _observers = observers;
    }

    public void Execute(
        ExecutionPlan<TState> plan,
        SimExecContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);

        for (var waveIndex = 0;
             waveIndex < plan.Waves.Count;
             waveIndex++)
        {
            ExecuteWave(
                plan.Waves[waveIndex],
                waveIndex,
                context);
        }
    }

    public async ValueTask ExecuteAsync(
        ExecutionPlan<TState> plan,
        SimExecContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        for (var waveIndex = 0;
             waveIndex < plan.Waves.Count;
             waveIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await ExecuteWaveAsync(
                    plan.Waves[waveIndex],
                    waveIndex,
                    context,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private void ExecuteWave(
        ExecutionWave<TState> wave,
        int waveIndex,
        SimExecContext context)
    {
        if (wave.Systems.Count == 0)
        {
            return;
        }

        if (ShouldExecuteSequentially(wave))
        {
            ExecuteSequentially(
                wave.Systems,
                waveIndex,
                context);

            return;
        }

        ExecuteInParallel(
            wave,
            waveIndex,
            context);
    }

    private async ValueTask ExecuteWaveAsync(
        ExecutionWave<TState> wave,
        int waveIndex,
        SimExecContext context,
        CancellationToken cancellationToken)
    {
        if (wave.Systems.Count == 0)
        {
            return;
        }

        if (ShouldExecuteSequentially(wave))
        {
            await ExecuteSequentiallyAsync(
                    wave.Systems,
                    waveIndex,
                    context,
                    cancellationToken)
                .ConfigureAwait(false);

            return;
        }

        await ExecuteInParallelAsync(
                wave,
                waveIndex,
                context,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private bool ShouldExecuteSequentially(
        ExecutionWave<TState> wave)
    {
        return wave.ExecutionMode == SimExecMode.Sequential
            || _options.MaxDegreeOfParallelism == 1
            || wave.Systems.Count == 1;
    }

    private void ExecuteSequentially(
        IReadOnlyList<ISimulationSystem<TState>> systems,
        int waveIndex,
        SimExecContext context)
    {
        foreach (var system in systems)
        {
            var execution = ExecuteSystem(
                system,
                waveIndex,
                context);

            execution.Commands.Apply(_state);
        }
    }

    private async ValueTask ExecuteSequentiallyAsync(
        IReadOnlyList<ISimulationSystem<TState>> systems,
        int waveIndex,
        SimExecContext context,
        CancellationToken cancellationToken)
    {
        foreach (var system in systems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var execution = await ExecuteSystemAsync(
                    system,
                    waveIndex,
                    context,
                    cancellationToken)
                .ConfigureAwait(false);

            execution.Commands.Apply(_state);
        }
    }

    private void ExecuteInParallel(
        ExecutionWave<TState> wave,
        int waveIndex,
        SimExecContext context)
    {
        var executions = new SystemExecution[wave.Systems.Count];

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism =
                _options.MaxDegreeOfParallelism
        };

        Parallel.For(
            0,
            wave.Systems.Count,
            parallelOptions,
            index =>
            {
                executions[index] = ExecuteSystem(
                    wave.Systems[index],
                    waveIndex,
                    context);
            });

        ApplyCommands(executions);
    }

    private async ValueTask ExecuteInParallelAsync(
        ExecutionWave<TState> wave,
        int waveIndex,
        SimExecContext context,
        CancellationToken cancellationToken)
    {
        var executions = new SystemExecution[wave.Systems.Count];

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism =
                _options.MaxDegreeOfParallelism,

            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(
                Enumerable.Range(0, wave.Systems.Count),
                parallelOptions,
                async (index, token) =>
                {
                    executions[index] = await ExecuteSystemAsync(
                            wave.Systems[index],
                            waveIndex,
                            context,
                            token)
                        .ConfigureAwait(false);
                })
            .ConfigureAwait(false);

        ApplyCommands(executions);
    }

    private void ApplyCommands(
        IReadOnlyList<SystemExecution> executions)
    {
        foreach (var execution in executions)
        {
            execution.Commands.Apply(_state);
        }
    }

    private SystemExecution ExecuteSystem(
        ISimulationSystem<TState> system,
        int waveIndex,
        SimExecContext context)
    {
        ArgumentNullException.ThrowIfNull(system);

        var commands = new CommandBuffer<TState>();

        var simulationContext = CreateSimulationContext(
            context,
            commands);

        var startedAt = _timeProvider.GetTimestamp();

        system.Execute(simulationContext);

        var metrics = CreateSystemMetrics(
            system,
            context,
            waveIndex,
            startedAt);

        NotifySystemCompleted(metrics);

        return new SystemExecution(commands);
    }

    private async ValueTask<SystemExecution> ExecuteSystemAsync(
        ISimulationSystem<TState> system,
        int waveIndex,
        SimExecContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(system);

        var commands = new CommandBuffer<TState>();

        var simulationContext = CreateSimulationContext(
            context,
            commands);

        var startedAt = _timeProvider.GetTimestamp();

        await system.ExecuteAsync(
                simulationContext,
                cancellationToken)
            .ConfigureAwait(false);

        var metrics = CreateSystemMetrics(
            system,
            context,
            waveIndex,
            startedAt);

        NotifySystemCompleted(metrics);

        return new SystemExecution(commands);
    }

    private SimContext<TState> CreateSimulationContext(
        SimExecContext context,
        CommandBuffer<TState> commands)
    {
        return new SimContext<TState>(
            _state,
            context.Tick,
            _simClock,
            context.Delta,
            context.Timestamp,
            commands);
    }

    private SystemMetrics CreateSystemMetrics(
        ISimulationSystem<TState> system,
        SimExecContext context,
        int waveIndex,
        long startedAt)
    {
        return new SystemMetrics(
            system.GetType().Name,
            context.Tick,
            _timeProvider.GetElapsedTime(startedAt),
            waveIndex);
    }

    private void NotifySystemCompleted(
        SystemMetrics metrics)
    {
        foreach (var observer in _observers)
        {
            observer.OnSystemCompleted(metrics);
        }
    }

    private sealed record SystemExecution(
        CommandBuffer<TState> Commands);
}