namespace Komissar.Systems;

public sealed class ExecutionPlan<TState>
{
	public static readonly ExecutionPlan<TState> Empty = new();

	public ExecutionPlan(IReadOnlyList<ExecutionWave<TState>> waves)
	{
		ArgumentNullException.ThrowIfNull(waves);

		Waves = waves;
	}

	private ExecutionPlan()
	{
		Waves = [];
	}
	
	public IReadOnlyList<ExecutionWave<TState>> Waves { get; }

	public int WaveCount => Waves.Count;
	public int SystemCount => Waves.Sum(wave => wave.Systems.Count);
}