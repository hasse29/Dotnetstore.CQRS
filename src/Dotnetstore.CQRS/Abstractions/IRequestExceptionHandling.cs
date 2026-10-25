namespace Dotnetstore.CQRS.Abstractions;

/// <summary>
/// Tracks whether an exception has been handled and, if so, which response replaces the failure.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class RequestExceptionHandlerState<TResponse>
{
    internal RequestExceptionHandlerState()
    {
    }

    /// <summary>Gets a value indicating whether a handler has marked the exception as handled.</summary>
    public bool Handled { get; private set; }

    /// <summary>Gets the response set by <see cref="SetHandled"/>.</summary>
    public TResponse? Response { get; private set; }

    /// <summary>Marks the exception as handled; <paramref name="response"/> is returned to the caller.</summary>
    /// <param name="response">The response to return instead of throwing.</param>
    public void SetHandled(TResponse response)
    {
        Handled = true;
        Response = response;
    }
}

/// <summary>
/// Handles an exception thrown while processing a request and may replace it with a response.
/// Handlers are looked up for the exception type and then each base type, most derived first,
/// and the first one to call <see cref="RequestExceptionHandlerState{TResponse}.SetHandled"/> wins.
/// If none does, the original exception is rethrown.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <typeparam name="TException">The exception type handled.</typeparam>
public interface IRequestExceptionHandler<in TRequest, TResponse, in TException>
    where TRequest : IRequest<TResponse>
    where TException : Exception
{
    /// <summary>Handles the exception.</summary>
    /// <param name="request">The request that failed.</param>
    /// <param name="exception">The exception.</param>
    /// <param name="state">Call <see cref="RequestExceptionHandlerState{TResponse}.SetHandled"/> to recover.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when handling is done.</returns>
    Task Handle(TRequest request, TException exception, RequestExceptionHandlerState<TResponse> state, CancellationToken cancellationToken);
}

/// <summary>
/// Reacts to an exception thrown while processing a request, for side effects such as logging.
/// Actions cannot recover from the exception and always run before any
/// <see cref="IRequestExceptionHandler{TRequest, TResponse, TException}"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TException">The exception type handled.</typeparam>
public interface IRequestExceptionAction<in TRequest, in TException>
    where TRequest : notnull
    where TException : Exception
{
    /// <summary>Runs the action.</summary>
    /// <param name="request">The request that failed.</param>
    /// <param name="exception">The exception.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the action is done.</returns>
    Task Execute(TRequest request, TException exception, CancellationToken cancellationToken);
}
