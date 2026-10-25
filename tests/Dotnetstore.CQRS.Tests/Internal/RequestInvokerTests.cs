using Dotnetstore.CQRS.Tests.Fixtures;

namespace Dotnetstore.CQRS.Tests.Internal;

public class RequestInvokerTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    // Behaviors

    [Fact]
    public async Task Invoke_WithBehaviors_RunsFirstRegisteredOutermost()
    {
        using var host = TestScope.Create(o => o
            .AddOpenBehavior(typeof(OuterBehavior<,>))
            .AddOpenBehavior(typeof(NamedBehavior<,>)));

        await host.Sender.Send(new Ping(), Token);

        host.Log.Entries.ShouldBe(["outer-before", "before", "ping", "after", "outer-after"]);
    }

    // Pre/post processors

    [Fact]
    public async Task Invoke_RunsPreThenBehaviorsThenHandlerThenPost()
    {
        using var host = TestScope.Create(o => o.AddOpenBehavior(typeof(NamedBehavior<,>)));

        var result = await host.Sender.Send(new Tracked(), Token);

        result.ShouldBe("ok");
        host.Log.Entries.ShouldBe(["pre", "before", "handler", "after", "post:ok"]);
    }

    [Fact]
    public async Task Invoke_OpenProcessors_ApplyToEveryRequest()
    {
        using var host = TestScope.Create(o => o
            .AddOpenPreProcessor(typeof(OpenPreProcessor<>))
            .AddOpenPostProcessor(typeof(OpenPostProcessor<,>)));

        await host.Sender.Send(new Ping(), Token);

        host.Log.Entries.ShouldBe(["open-pre", "ping", "open-post"]);
    }

    // Exception handling

    [Fact]
    public async Task Invoke_MostSpecificExceptionHandlerWins_AfterActions()
    {
        using var host = TestScope.Create();

        var result = await host.Sender.Send(new Failing(new ArgumentNullException("x")), Token);

        result.ShouldBe("specific");
        host.Log.Entries.ShouldBe(["action", "specific-handler"]);
    }

    [Fact]
    public async Task Invoke_FallsBackToBaseExceptionHandler()
    {
        using var host = TestScope.Create();

        var result = await host.Sender.Send(new Failing(new InvalidOperationException()), Token);

        result.ShouldBe("general");
    }

    [Fact]
    public async Task Invoke_UnhandledException_RunsActionsAndRethrowsOriginal()
    {
        using var host = TestScope.Create();
        var error = new InvalidOperationException("boom");

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => host.Sender.Send(new FailingUnhandled(error), Token));

        thrown.ShouldBeSameAs(error);
        host.Log.Entries.ShouldBe(["action"]);
    }

    [Fact]
    public async Task Invoke_HandlerThatDoesNotSetHandled_Rethrows()
    {
        using var host = TestScope.Create();

        await Should.ThrowAsync<ArgumentException>(() => host.Sender.Send(new FailingUnhandled(new ArgumentException("x")), Token));
    }
}
