namespace Komissar.Systems;

public sealed class SystemDependencyGraph
{
	private readonly Dictionary<Type, HashSet<Type>> _dependencies = [];
	private readonly Dictionary<Type, HashSet<Type>> _dependents = [];

	public IReadOnlyCollection<Type> Systems => _dependencies.Keys;

	public IReadOnlyCollection<Type> GetDependencies(Type system)
	{
		ArgumentNullException.ThrowIfNull(system);

		return _dependencies.TryGetValue(system, out var dependencies)
			? dependencies
			: [];
	}

	public IReadOnlyCollection<Type> GetDependents(Type system)
	{
		ArgumentNullException.ThrowIfNull(system);

		return _dependents.TryGetValue(system, out var dependents)
			? dependents
			: [];
	}

	public void AddSystem(Type system)
	{
		ArgumentNullException.ThrowIfNull(system);

		if (_dependencies.ContainsKey(system))
			return;

		_dependencies.Add(system, []);
		_dependents.Add(system, []);
	}

	public void AddDependency(Type system, Type dependency)
	{
		ArgumentNullException.ThrowIfNull(system);
		ArgumentNullException.ThrowIfNull(dependency);

		AddSystem(system);
		AddSystem(dependency);

		if (!_dependencies[system].Add(dependency))
			return;

		_dependents[dependency].Add(system);
	}

	public bool HasDependency(Type system, Type dependency)
	{
		ArgumentNullException.ThrowIfNull(system);
		ArgumentNullException.ThrowIfNull(dependency);

		return _dependencies.TryGetValue(system, out var dependencies) && dependencies.Contains(dependency);
	}
}