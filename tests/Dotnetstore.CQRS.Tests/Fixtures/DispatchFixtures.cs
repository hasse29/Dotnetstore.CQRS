using Dotnetstore.CQRS.Abstractions;

namespace Dotnetstore.CQRS.Tests.Fixtures;

internal sealed record CreateOrder(string Name) : ICommand<int>;

internal sealed class CreateOrderHandler : ICommandHandler<CreateOrder, int>
{
    public Task<int> Handle(CreateOrder request, CancellationToken cancellationToken) =>
        Task.FromResult(request.Name.Length);
}

internal sealed record Ping : ICommand;

internal sealed class PingHandler(CallLog log) : ICommandHandler<Ping, Unit>
{
    public Task<Unit> Handle(Ping request, CancellationToken cancellationToken)
    {
        log.Add("ping");
        return Unit.Task;
    }
}

internal sealed record GetGreeting(string Name) : IQuery<string>;

internal sealed class GetGreetingHandler : IQueryHandler<GetGreeting, string>
{
    public Task<string> Handle(GetGreeting request, CancellationToken cancellationToken) =>
        Task.FromResult($"Hello, {request.Name}");
}

internal sealed record Unhandled : IQuery<int>;

internal sealed record OrderPlaced(int Id) : INotification;

internal sealed class FirstOrderPlacedHandler(CallLog log) : INotificationHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        log.Add($"first:{notification.Id}");
        return Task.CompletedTask;
    }
}

internal sealed class SecondOrderPlacedHandler(CallLog log) : INotificationHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        log.Add($"second:{notification.Id}");
        return Task.CompletedTask;
    }
}

internal sealed class NamedBehavior<TRequest, TResponse>(CallLog log) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        log.Add("before");
        var response = await next().ConfigureAwait(false);
        log.Add("after");
        return response;
    }
}

internal sealed class OuterBehavior<TRequest, TResponse>(CallLog log) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        log.Add("outer-before");
        var response = await next().ConfigureAwait(false);
        log.Add("outer-after");
        return response;
    }
}
