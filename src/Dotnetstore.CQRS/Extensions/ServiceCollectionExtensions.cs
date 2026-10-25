using Dotnetstore.CQRS.Abstractions;
using Dotnetstore.CQRS.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dotnetstore.CQRS;

/// <summary>
/// DI registration for Dotnetstore.CQRS.
/// </summary>
public static class ServiceCollectionExtensions
{
    private static readonly HashSet<Type> HandlerContracts =
    [
        typeof(IRequestHandler<,>),
        typeof(IStreamRequestHandler<,>),
        typeof(INotificationHandler<>),
        typeof(IRequestPreProcessor<>),
        typeof(IRequestPostProcessor<,>),
        typeof(IRequestExceptionHandler<,,>),
        typeof(IRequestExceptionAction<,>),
    ];

    /// <summary>
    /// Registers <see cref="ISender"/>, <see cref="IPublisher"/>, everything found in the configured
    /// assemblies, and any open generic behaviors and processors.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the assemblies to scan, pipeline behaviors and publish strategy.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddCqrs(this IServiceCollection services, Action<CqrsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new CqrsOptions();
        configure(options);

        services.TryAddSingleton(new DispatcherSettings(options.NotificationPublishStrategy));
        services.TryAddScoped<Dispatcher>();
        services.TryAddScoped<ISender>(static sp => sp.GetRequiredService<Dispatcher>());
        services.TryAddScoped<IPublisher>(static sp => sp.GetRequiredService<Dispatcher>());

        foreach (var assembly in options.Assemblies.Distinct())
        {
            foreach (var type in assembly.GetTypes().Where(static t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }))
            {
                foreach (var contract in type.GetInterfaces().Where(static i => i.IsGenericType && HandlerContracts.Contains(i.GetGenericTypeDefinition())))
                {
                    services.AddTransient(contract, type);
                }
            }
        }

        foreach (var (service, implementation) in options.OpenRegistrations)
        {
            services.AddTransient(service, implementation);
        }

        return services;
    }
}
