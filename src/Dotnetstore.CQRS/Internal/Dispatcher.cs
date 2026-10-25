using System.Collections.Concurrent;
using Dotnetstore.CQRS.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.CQRS.Internal;

internal sealed record DispatcherSettings(NotificationPublishStrategy NotificationPublishStrategy);

internal sealed class Dispatcher(IServiceProvider services, DispatcherSettings settings) : ISender, IPublisher
{
    private static readonly ConcurrentDictionary<Type, object> RequestInvokers = new();
    private static readonly ConcurrentDictionary<Type, object> StreamInvokers = new();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var invoker = (RequestInvoker<TResponse>)RequestInvokers.GetOrAdd(request.GetType(), static (requestType, _) =>
            Activator.CreateInstance(typeof(RequestInvoker<,>).MakeGenericType(requestType, typeof(TResponse)))!, 0);

        return invoker.Invoke(request, services, cancellationToken);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var invoker = (StreamInvoker<TResponse>)StreamInvokers.GetOrAdd(request.GetType(), static (requestType, _) =>
            Activator.CreateInstance(typeof(StreamInvoker<,>).MakeGenericType(requestType, typeof(TResponse)))!, 0);

        return invoker.Invoke(request, services, cancellationToken);
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification =>
        Publish(notification, settings.NotificationPublishStrategy, cancellationToken);

    public async Task Publish<TNotification>(
        TNotification notification,
        NotificationPublishStrategy strategy,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var handlers = services.GetServices<INotificationHandler<TNotification>>();

        switch (strategy)
        {
            case NotificationPublishStrategy.Sequential:
                foreach (var handler in handlers)
                {
                    await handler.Handle(notification, cancellationToken).ConfigureAwait(false);
                }

                break;

            case NotificationPublishStrategy.Parallel:
                await PublishInParallel(handlers, notification, cancellationToken).ConfigureAwait(false);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Unknown notification publish strategy.");
        }
    }

    private static async Task PublishInParallel<TNotification>(
        IEnumerable<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        // The wrapper turns synchronous throws from Handle into faulted tasks so every handler still runs.
        static async Task Run(INotificationHandler<TNotification> handler, TNotification notification, CancellationToken cancellationToken) =>
            await handler.Handle(notification, cancellationToken).ConfigureAwait(false);

        var all = Task.WhenAll(handlers.Select(handler => Run(handler, notification, cancellationToken)));

        try
        {
            await all.ConfigureAwait(false);
        }
        catch when (all.Exception is { InnerExceptions.Count: > 1 })
        {
            throw all.Exception;
        }
    }
}
