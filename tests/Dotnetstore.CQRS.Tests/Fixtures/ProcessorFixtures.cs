using Dotnetstore.CQRS.Abstractions;

namespace Dotnetstore.CQRS.Tests.Fixtures;

internal sealed record Tracked : ICommand<string>;

internal sealed class TrackedHandler(CallLog log) : ICommandHandler<Tracked, string>
{
    public Task<string> Handle(Tracked request, CancellationToken cancellationToken)
    {
        log.Add("handler");
        return Task.FromResult("ok");
    }
}

internal sealed class TrackedPreProcessor(CallLog log) : IRequestPreProcessor<Tracked>
{
    public Task Process(Tracked request, CancellationToken cancellationToken)
    {
        log.Add("pre");
        return Task.CompletedTask;
    }
}

internal sealed class TrackedPostProcessor(CallLog log) : IRequestPostProcessor<Tracked, string>
{
    public Task Process(Tracked request, string response, CancellationToken cancellationToken)
    {
        log.Add($"post:{response}");
        return Task.CompletedTask;
    }
}

internal sealed class OpenPreProcessor<TRequest>(CallLog log) : IRequestPreProcessor<TRequest>
    where TRequest : notnull
{
    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        log.Add("open-pre");
        return Task.CompletedTask;
    }
}

internal sealed class OpenPostProcessor<TRequest, TResponse>(CallLog log) : IRequestPostProcessor<TRequest, TResponse>
    where TRequest : notnull
{
    public Task Process(TRequest request, TResponse response, CancellationToken cancellationToken)
    {
        log.Add("open-post");
        return Task.CompletedTask;
    }
}
