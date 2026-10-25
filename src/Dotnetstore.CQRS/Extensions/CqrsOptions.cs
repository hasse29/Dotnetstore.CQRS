using System.Reflection;
using Dotnetstore.CQRS.Abstractions;

namespace Dotnetstore.CQRS;

/// <summary>
/// Configures which handlers, processors and pipeline behaviors <c>AddCqrs</c> registers.
/// </summary>
public sealed class CqrsOptions
{
    internal List<Assembly> Assemblies { get; } = [];

    internal List<(Type Service, Type Implementation)> OpenRegistrations { get; } = [];

    /// <summary>
    /// Gets or sets how <see cref="IPublisher.Publish{TNotification}(TNotification, CancellationToken)"/> runs handlers.
    /// Defaults to <see cref="NotificationPublishStrategy.Sequential"/>.
    /// </summary>
    public NotificationPublishStrategy NotificationPublishStrategy { get; set; } = NotificationPublishStrategy.Sequential;

    /// <summary>
    /// Scans <paramref name="assembly"/> for request, stream, notification, pre/post-processor and
    /// exception handler/action implementations.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>This instance, for chaining.</returns>
    public CqrsOptions AddHandlersFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        Assemblies.Add(assembly);
        return this;
    }

    /// <summary>Scans the assembly containing <typeparamref name="T"/>; see <see cref="AddHandlersFromAssembly"/>.</summary>
    /// <typeparam name="T">A type in the assembly to scan.</typeparam>
    /// <returns>This instance, for chaining.</returns>
    public CqrsOptions AddHandlersFromAssemblyContaining<T>() => AddHandlersFromAssembly(typeof(T).Assembly);

    /// <summary>
    /// Adds an open generic pipeline behavior, for example <c>typeof(LoggingBehavior&lt;,&gt;)</c>.
    /// Behaviors run in the order they are added, the first being the outermost.
    /// </summary>
    /// <param name="openBehaviorType">An open generic type implementing <see cref="IPipelineBehavior{TRequest, TResponse}"/>.</param>
    /// <returns>This instance, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="openBehaviorType"/> is not an open generic behavior type.</exception>
    public CqrsOptions AddOpenBehavior(Type openBehaviorType) =>
        AddOpen(typeof(IPipelineBehavior<,>), openBehaviorType);

    /// <summary>Adds an open generic <see cref="IRequestPreProcessor{TRequest}"/> that applies to every request.</summary>
    /// <param name="openPreProcessorType">An open generic type implementing <see cref="IRequestPreProcessor{TRequest}"/>.</param>
    /// <returns>This instance, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="openPreProcessorType"/> is not an open generic pre-processor type.</exception>
    public CqrsOptions AddOpenPreProcessor(Type openPreProcessorType) =>
        AddOpen(typeof(IRequestPreProcessor<>), openPreProcessorType);

    /// <summary>Adds an open generic <see cref="IRequestPostProcessor{TRequest, TResponse}"/> that applies to every request.</summary>
    /// <param name="openPostProcessorType">An open generic type implementing <see cref="IRequestPostProcessor{TRequest, TResponse}"/>.</param>
    /// <returns>This instance, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="openPostProcessorType"/> is not an open generic post-processor type.</exception>
    public CqrsOptions AddOpenPostProcessor(Type openPostProcessorType) =>
        AddOpen(typeof(IRequestPostProcessor<,>), openPostProcessorType);

    private CqrsOptions AddOpen(Type service, Type implementation)
    {
        ArgumentNullException.ThrowIfNull(implementation);

        if (!implementation.IsGenericTypeDefinition
            || !implementation.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == service))
        {
            throw new ArgumentException(
                $"'{implementation}' must be an open generic type implementing {service.Name.Split('`')[0]}<{new string(',', service.GetGenericArguments().Length - 1)}>.",
                nameof(implementation));
        }

        OpenRegistrations.Add((service, implementation));
        return this;
    }
}
