namespace Dotnetstore.CQRS.Abstractions;

/// <summary>
/// Runs before a request enters the rest of the pipeline. All pre-processors run in registration order,
/// before any <see cref="IPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
public interface IRequestPreProcessor<in TRequest>
    where TRequest : notnull
{
    /// <summary>Processes the request.</summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when processing is done.</returns>
    Task Process(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Runs after the handler and all pipeline behaviors have produced a response. All post-processors run in
/// registration order. They do not run when the request throws.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IRequestPostProcessor<in TRequest, in TResponse>
    where TRequest : notnull
{
    /// <summary>Processes the request and its response.</summary>
    /// <param name="request">The request.</param>
    /// <param name="response">The response.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when processing is done.</returns>
    Task Process(TRequest request, TResponse response, CancellationToken cancellationToken);
}
