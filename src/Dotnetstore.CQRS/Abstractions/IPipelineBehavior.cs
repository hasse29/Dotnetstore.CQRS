namespace Dotnetstore.CQRS.Abstractions;

/// <summary>
/// Invokes the next step of the pipeline, ending at the request handler.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <returns>The response.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Wraps request handling, for cross-cutting concerns such as validation, logging or transactions.
/// Behaviors run in registration order, the first registered being the outermost.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Handles the request, calling <paramref name="next"/> to continue the pipeline.</summary>
    /// <param name="request">The request.</param>
    /// <param name="next">The next step in the pipeline.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response.</returns>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
