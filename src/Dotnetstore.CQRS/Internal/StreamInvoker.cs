using Dotnetstore.CQRS.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.CQRS.Internal;

internal abstract class StreamInvoker<TResponse>
{
    public abstract IAsyncEnumerable<TResponse> Invoke(object request, IServiceProvider services, CancellationToken cancellationToken);
}

internal sealed class StreamInvoker<TRequest, TResponse> : StreamInvoker<TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    public override IAsyncEnumerable<TResponse> Invoke(object request, IServiceProvider services, CancellationToken cancellationToken)
    {
        var handler = services.GetService<IStreamRequestHandler<TRequest, TResponse>>()
            ?? throw new InvalidOperationException(
                $"No handler registered for stream request type '{typeof(TRequest)}'.");

        return handler.Handle((TRequest)request, cancellationToken);
    }
}
