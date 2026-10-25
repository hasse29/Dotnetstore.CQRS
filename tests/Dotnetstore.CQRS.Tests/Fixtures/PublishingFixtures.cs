using Dotnetstore.CQRS.Abstractions;

namespace Dotnetstore.CQRS.Tests.Fixtures;

internal sealed class Rendezvous
{
    private readonly TaskCompletionSource _first = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _second = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task FirstArrives()
    {
        _first.SetResult();
        await _second.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
    }

    public async Task SecondArrives()
    {
        _second.SetResult();
        await _first.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
    }
}

internal sealed record Rendezvoused : INotification;

internal sealed class RendezvousFirstHandler(Rendezvous rendezvous) : INotificationHandler<Rendezvoused>
{
    public Task Handle(Rendezvoused notification, CancellationToken cancellationToken) => rendezvous.FirstArrives();
}

internal sealed class RendezvousSecondHandler(Rendezvous rendezvous) : INotificationHandler<Rendezvoused>
{
    public Task Handle(Rendezvoused notification, CancellationToken cancellationToken) => rendezvous.SecondArrives();
}

internal sealed record Failed : INotification;

internal sealed class FailedFirstHandler : INotificationHandler<Failed>
{
    public Task Handle(Failed notification, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("first");
}

internal sealed class FailedSecondHandler : INotificationHandler<Failed>
{
    public Task Handle(Failed notification, CancellationToken cancellationToken) =>
        throw new ArgumentException("second");
}

internal sealed class FailedThirdHandler(CallLog log) : INotificationHandler<Failed>
{
    public Task Handle(Failed notification, CancellationToken cancellationToken)
    {
        log.Add("third");
        return Task.CompletedTask;
    }
}
