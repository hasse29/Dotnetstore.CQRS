using System.Collections.Concurrent;
using Dotnetstore.CQRS.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.CQRS.Internal;

internal abstract class RequestInvoker<TResponse>
{
    public abstract Task<TResponse> Invoke(object request, IServiceProvider services, CancellationToken cancellationToken);
}

internal sealed class RequestInvoker<TRequest, TResponse> : RequestInvoker<TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ConcurrentDictionary<Type, ExceptionInvoker<TRequest, TResponse>> ExceptionInvokers = new();

    public override async Task<TResponse> Invoke(object request, IServiceProvider services, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;

        var handler = services.GetService<IRequestHandler<TRequest, TResponse>>()
            ?? throw new InvalidOperationException(
                $"No handler registered for request type '{typeof(TRequest)}'.");

        RequestHandlerDelegate<TResponse> pipeline = () => handler.Handle(typedRequest, cancellationToken);

        var behaviors = services.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToArray();
        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var next = pipeline;
            pipeline = () => behavior.Handle(typedRequest, next, cancellationToken);
        }

        var preProcessors = services.GetServices<IRequestPreProcessor<TRequest>>().ToArray();
        var postProcessors = services.GetServices<IRequestPostProcessor<TRequest, TResponse>>().ToArray();

        try
        {
            foreach (var preProcessor in preProcessors)
            {
                await preProcessor.Process(typedRequest, cancellationToken).ConfigureAwait(false);
            }

            var response = await pipeline().ConfigureAwait(false);

            foreach (var postProcessor in postProcessors)
            {
                await postProcessor.Process(typedRequest, response, cancellationToken).ConfigureAwait(false);
            }

            return response;
        }
#pragma warning disable CA1031 // Any exception may be offered to exception handlers; unhandled ones are rethrown
        catch (Exception exception)
#pragma warning restore CA1031
        {
            var state = await HandleException(typedRequest, exception, services, cancellationToken).ConfigureAwait(false);
            if (state.Handled)
            {
                return state.Response!;
            }

            throw;
        }
    }

    private static async Task<RequestExceptionHandlerState<TResponse>> HandleException(
        TRequest request,
        Exception exception,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var state = new RequestExceptionHandlerState<TResponse>();

        var invokers = new List<ExceptionInvoker<TRequest, TResponse>>();
        for (var type = exception.GetType(); type is not null && type != typeof(object); type = type.BaseType)
        {
            invokers.Add(ExceptionInvokers.GetOrAdd(type, static exceptionType =>
                (ExceptionInvoker<TRequest, TResponse>)Activator.CreateInstance(
                    typeof(ExceptionInvoker<,,>).MakeGenericType(typeof(TRequest), typeof(TResponse), exceptionType))!));
        }

        foreach (var invoker in invokers)
        {
            await invoker.RunActions(request, exception, services, cancellationToken).ConfigureAwait(false);
        }

        foreach (var invoker in invokers)
        {
            await invoker.RunHandlers(request, exception, state, services, cancellationToken).ConfigureAwait(false);
            if (state.Handled)
            {
                break;
            }
        }

        return state;
    }
}

internal abstract class ExceptionInvoker<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public abstract Task RunActions(TRequest request, Exception exception, IServiceProvider services, CancellationToken cancellationToken);

    public abstract Task RunHandlers(
        TRequest request,
        Exception exception,
        RequestExceptionHandlerState<TResponse> state,
        IServiceProvider services,
        CancellationToken cancellationToken);
}

internal sealed class ExceptionInvoker<TRequest, TResponse, TException> : ExceptionInvoker<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TException : Exception
{
    public override async Task RunActions(TRequest request, Exception exception, IServiceProvider services, CancellationToken cancellationToken)
    {
        foreach (var action in services.GetServices<IRequestExceptionAction<TRequest, TException>>())
        {
            await action.Execute(request, (TException)exception, cancellationToken).ConfigureAwait(false);
        }
    }

    public override async Task RunHandlers(
        TRequest request,
        Exception exception,
        RequestExceptionHandlerState<TResponse> state,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        foreach (var handler in services.GetServices<IRequestExceptionHandler<TRequest, TResponse, TException>>())
        {
            await handler.Handle(request, (TException)exception, state, cancellationToken).ConfigureAwait(false);
            if (state.Handled)
            {
                return;
            }
        }
    }
}
