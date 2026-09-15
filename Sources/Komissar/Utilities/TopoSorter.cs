using Komissar.Systems;

namespace Komissar.Utilities;

public sealed class TopoSorter
{
	public IReadOnlyList<IReadOnlyList<Type>> Sort(SystemDependencyGraph graph)
	{
		ArgumentNullException.ThrowIfNull(graph);

		var dependencies = graph.Systems.ToDictionary(system => system, system => graph.GetDependencies(system).Count);
		var waves = new List<IReadOnlyList<Type>>();

		while (dependencies.Count > 0)
		{
			var ready = dependencies
			            .Where(pair => pair.Value == 0)
			            .Select(pair => pair.Key)
			            .ToList();

			if (ready.Count == 0)
				throw new InvalidOperationException("Circular dependency detected in simulation systems.");

			waves.Add(ready.AsReadOnly());

			foreach (var system in ready)
			{
				dependencies.Remove(system);

				foreach (var dependent in graph.GetDependents(system))
				{
					dependencies[dependent]--;
				}
			}
		}

		return waves.AsReadOnly();
	}
}