using System.Collections.ObjectModel;
using Komissar.Core.Enums;
using Komissar.Diagnostics.Interfaces;
using Komissar.Diagnostics.Metrics;
using Komissar.Runtime.Interfaces;
using Komissar.Systems;
using Komissar.Systems.Interfaces;
using Komissar.Utilities;

namespace Komissar.Runtime;

public abstract class SimBase<TState> : ISim<TState>
{
	private SimStatus _status = SimStatus.Created;

	private long _tick;
	private int _disposed;

	protected SimBase(
		TState state,
		SimOptions options,
		SystemPlanner<TState> planner,
		IEnumerable<ISimulationSystem<TState>> systems,
		TimeProvider? timeProvider = null,
		SimClock? clock = null,
		TimeScale? timeScale = null,
		IEnumerable<ISimObserver<TState>>? observers = null)
	{
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(planner);
		ArgumentNullException.ThrowIfNull(systems);

		ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxDegreeOfParallelism, 1);
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(options.TickInterval, TimeSpan.Zero);

		var systemList = systems.ToList();

		Options = options;
		TimeProvider = timeProvider ?? TimeProvider.System;

		SimClock = clock ?? new SimClock();
		TimeScale = timeScale ?? new TimeScale();

		Observers = new ReadOnlyCollection<ISimObserver<TState>>([.. observers ?? []]);

		ExecutionPlan = planner.Build(systemList);
		Executor = new SimExecutor<TState>(state, SimClock, Options, TimeProvider, Observers);
	}

	public SimMode Mode => Options.Mode;

	public SimStatus Status { get { lock (StatusLock) return _status; } }

	public long Tick => Interlocked.Read(ref _tick);

	public SimClock SimClock { get; }

	public TimeScale TimeScale { get; }

	protected SimOptions Options { get; }

	protected TimeProvider TimeProvider { get; }

	protected ExecutionPlan<TState> ExecutionPlan { get; }

	protected SimExecutor<TState> Executor { get; }

	protected IReadOnlyList<ISimObserver<TState>> Observers { get; }

	protected Lock StatusLock { get; } = new();

	public abstract void Start();

	public abstract void Pause();

	public abstract void Stop();

	public abstract void Step();

	public abstract void Step(TimeSpan delta);

	public abstract void Update(TimeSpan realDelta);

	public abstract void Dispose();

	protected void StartCore()
	{
		lock (StatusLock)
		{
			SimpleValidator.EnsureCanStart(_status);
			_status = SimStatus.Running;
		}
	}

	protected void PauseCore()
	{
		lock (StatusLock)
		{
			if (_status == SimStatus.Running)
			{
				_status = SimStatus.Paused;
			}
		}
	}

	protected void StopCore()
	{
		lock (StatusLock)
		{
			if (_status != SimStatus.Stopped)
			{
				_status = SimStatus.Stopped;
			}
		}
	}

	protected void EnsureCanExecuteStep()
	{
		lock (StatusLock)
		{
			SimpleValidator.EnsureCanStep(_status);
		}
	}

	protected bool IsRunning()
	{
		lock (StatusLock)
		{
			return _status == SimStatus.Running;
		}
	}

	protected void CommitTick(long tick, TimeSpan delta)
	{
		SimClock.Advance(delta);

		Interlocked.Exchange(ref _tick, tick);
	}

	protected TickMetrics CreateTickMetrics(long tick, TimeSpan delta, long startedAt)
	{
		return new TickMetrics(tick, delta, TimeProvider.GetElapsedTime(startedAt), ExecutionPlan.SystemCount, ExecutionPlan.WaveCount);
	}

	protected void NotifyTickCompleted(TickMetrics metrics)
	{
		foreach (var observer in Observers)
		{
			observer.OnTickCompleted(metrics);
		}
	}

	protected void ThrowIfDisposed()
	{
		ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
	}

	protected bool TryMarkDisposed()
	{
		return Interlocked.Exchange(ref _disposed, 1) == 0;
	}
}