namespace Dotnetstore.CQRS.Abstractions;

/// <summary>
/// A request answered by a stream of responses instead of a single one.
/// </summary>
/// <typeparam name="TResponse">The type of each streamed item.</typeparam>
public interface IStreamRequest<out TResponse>;

/// <summary>
/// Handles a stream request.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The type of each streamed item.</typeparam>
public interface IStreamRequestHandler<in TRequest, out TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    /// <summary>Handles the request.</summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">A token to cancel the stream.</param>
    /// <returns>The stream of responses.</returns>
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
