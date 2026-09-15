using Komissar.Core.Attributes;
using Komissar.Core.Enums;
using Komissar.Systems.Interfaces;
using Komissar.Utilities;

namespace Komissar.Systems;

public sealed class SystemPlanner<TState>
{
    private readonly TopoSorter _sorter;

    public SystemPlanner(TopoSorter sorter)
    {
        _sorter = sorter;
    }

    public ExecutionPlan<TState> Build(IEnumerable<ISimulationSystem<TState>> systems)
    {
        ArgumentNullException.ThrowIfNull(systems);

        var systemList = systems.ToList();

        if (systemList.Count == 0)
            return ExecutionPlan<TState>.Empty;

        ValidateSystems(systemList);

        var graph = BuildDependencyGraph(systemList);

        ValidateGraph(graph);

        var levels = _sorter.Sort(graph);
        var waves = BuildExecutionWaves(systemList, levels);

        return new ExecutionPlan<TState>(waves);
    }

    private static void ValidateSystems(IReadOnlyList<ISimulationSystem<TState>> systems)
    {
        var duplicates = systems
            .GroupBy(system => system.GetType())
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicates.Count == 0)
            return;

        var names = string.Join(", ", duplicates.Select(type => type.Name));

        throw new InvalidOperationException($"Duplicate simulation systems detected: {names}.");
    }

    private static SystemDependencyGraph BuildDependencyGraph(
        IReadOnlyList<ISimulationSystem<TState>> systems)
    {
        var graph = new SystemDependencyGraph();

        var registeredTypes = systems
            .Select(system => system.GetType())
            .ToHashSet();

        foreach (var system in systems)
        {
            var systemType = system.GetType();

            graph.AddSystem(systemType);

            var attributes = systemType
                .GetCustomAttributes(
                    inherit: false);

            foreach (var attribute in attributes)
            {
                switch (attribute)
                {
                    case RunsAfterAttribute runsAfter:
                    {
                        ValidateDependency(systemType, runsAfter.SystemType, registeredTypes);
                        graph.AddDependency(systemType, runsAfter.SystemType);
                        break;
                    }

                    case RunsBeforeAttribute runsBefore:
                    {
                        ValidateDependency(systemType, runsBefore.SystemType, registeredTypes);
                        graph.AddDependency(runsBefore.SystemType, systemType);
                        break;
                    }
                }
            }
        }

        return graph;
    }

    private static void ValidateDependency(Type systemType, Type dependency, IReadOnlySet<Type> registeredTypes)
    {
        if (registeredTypes.Contains(dependency))
            return;

        throw new InvalidOperationException(
            $"System '{systemType.Name}' references " +
            $"unregistered system '{dependency.Name}'.");
    }

    private static void ValidateGraph(SystemDependencyGraph graph)
    {
        
    }

    private static IReadOnlyList<ExecutionWave<TState>> BuildExecutionWaves(
        IReadOnlyList<ISimulationSystem<TState>> systems,
        IReadOnlyList<IReadOnlyList<Type>> levels)
    {
        var systemsByType = systems.ToDictionary(
            system => system.GetType());

        var waves = new List<ExecutionWave<TState>>(
            levels.Count);

        for (var index = 0; index < levels.Count; index++)
        {
            var level = levels[index];

            var waveSystems = level
                .Select(type => systemsByType[type])
                .ToList()
                .AsReadOnly();

            var executionMode = DetermineExecutionMode(waveSystems);

            waves.Add(new ExecutionWave<TState>(index, waveSystems, executionMode));
        }

        return waves.AsReadOnly();
    }

    private static SimExecMode DetermineExecutionMode(IReadOnlyList<ISimulationSystem<TState>> systems)
    {
        if (systems.Count <= 1)
            return SimExecMode.Sequential;

        var hasSequential = systems.Any(system => system.ExecutionMode == SimExecMode.Sequential);

        if (hasSequential)
            return SimExecMode.Sequential;

        return SimExecMode.Parallel;
    }
}