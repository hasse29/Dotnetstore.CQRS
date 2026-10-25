using Dotnetstore.CQRS.Abstractions;

namespace Dotnetstore.CQRS.Tests.Fixtures;

internal sealed record Failing(Exception Error) : ICommand<string>;

internal sealed class FailingHandler : ICommandHandler<Failing, string>
{
    public Task<string> Handle(Failing request, CancellationToken cancellationToken) => throw request.Error;
}

internal sealed class FailingGeneralHandler : IRequestExceptionHandler<Failing, string, Exception>
{
    public Task Handle(Failing request, Exception exception, RequestExceptionHandlerState<string> state, CancellationToken cancellationToken)
    {
        state.SetHandled("general");
        return Task.CompletedTask;
    }
}

internal sealed class FailingSpecificHandler(CallLog log) : IRequestExceptionHandler<Failing, string, ArgumentNullException>
{
    public Task Handle(Failing request, ArgumentNullException exception, RequestExceptionHandlerState<string> state, CancellationToken cancellationToken)
    {
        log.Add("specific-handler");
        state.SetHandled("specific");
        return Task.CompletedTask;
    }
}

internal sealed class FailingAction(CallLog log) : IRequestExceptionAction<Failing, Exception>
{
    public Task Execute(Failing request, Exception exception, CancellationToken cancellationToken)
    {
        log.Add("action");
        return Task.CompletedTask;
    }
}

internal sealed record FailingUnhandled(Exception Error) : ICommand<string>;

internal sealed class FailingUnhandledHandler : ICommandHandler<FailingUnhandled, string>
{
    public Task<string> Handle(FailingUnhandled request, CancellationToken cancellationToken) => throw request.Error;
}

internal sealed class FailingUnhandledAction(CallLog log) : IRequestExceptionAction<FailingUnhandled, Exception>
{
    public Task Execute(FailingUnhandled request, Exception exception, CancellationToken cancellationToken)
    {
        log.Add("action");
        return Task.CompletedTask;
    }
}

internal sealed class NoOpHandlerState : IRequestExceptionHandler<FailingUnhandled, string, ArgumentException>
{
    public Task Handle(FailingUnhandled request, ArgumentException exception, RequestExceptionHandlerState<string> state, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
