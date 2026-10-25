namespace Dotnetstore.CQRS.Abstractions;

/// <summary>
/// Base marker for a request that is handled by exactly one handler and produces a response.
/// Implement <see cref="ICommand{TResponse}"/> or <see cref="IQuery{TResponse}"/> instead of this interface directly.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IRequest<out TResponse>;

/// <summary>
/// Handles a single request type.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Handles the request.</summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response.</returns>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
