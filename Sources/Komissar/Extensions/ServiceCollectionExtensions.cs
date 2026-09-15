using Komissar.Diagnostics;
using Komissar.Diagnostics.Interfaces;
using Komissar.Runtime;
using Komissar.Runtime.Interfaces;
using Komissar.Systems;
using Komissar.Systems.Interfaces;
using Komissar.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Komissar.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKomissar<TState>(this IServiceCollection services, Action<SimOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new SimOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<TimeScale>();
        services.TryAddSingleton<SimClock>();
        
        services.TryAddSingleton<SimExecutor<TState>>();
        services.TryAddSingleton<SystemPlanner<TState>>();
        services.TryAddSingleton<TopoSorter>();

        services.TryAddSingleton<ISim<TState>, SimSync<TState>>();
        services.TryAddSingleton<ISimObserver<TState>, MetricsCollector<TState>>();

        return services;
    }
}
