namespace Dotnetstore.CQRS.Abstractions;

/// <summary>
/// Sends commands and queries to their single handler.
/// </summary>
public interface ISender
{
    /// <summary>Sends a request through the pipeline to its handler.</summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The handler's response.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No handler is registered for the request type.</exception>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>Sends a stream request to its handler. Pipeline behaviors and processors do not apply to streams.</summary>
    /// <typeparam name="TResponse">The type of each streamed item.</typeparam>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">A token to cancel the stream.</param>
    /// <returns>The handler's stream of responses.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No handler is registered for the request type.</exception>
    IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Publishes notifications to all registered handlers.
/// </summary>
public interface IPublisher
{
    /// <summary>Publishes a notification using the strategy configured in <c>AddCqrs</c> (sequential by default).</summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when all handlers have run.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="notification"/> is <see langword="null"/>.</exception>
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;

    /// <summary>Publishes a notification using an explicit strategy.</summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <param name="notification">The notification.</param>
    /// <param name="strategy">How the handlers are run.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when all handlers have run.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="notification"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="strategy"/> is not a defined value.</exception>
    Task Publish<TNotification>(TNotification notification, NotificationPublishStrategy strategy, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
