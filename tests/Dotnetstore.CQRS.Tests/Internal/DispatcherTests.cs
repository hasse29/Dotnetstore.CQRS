using Dotnetstore.CQRS.Abstractions;
using Dotnetstore.CQRS.Tests.Fixtures;

namespace Dotnetstore.CQRS.Tests.Internal;

public class DispatcherTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    // Send

    [Fact]
    public async Task Send_Command_ReturnsHandlerResponse()
    {
        using var host = TestScope.Create();

        var result = await host.Sender.Send(new CreateOrder("abcd"), Token);

        result.ShouldBe(4);
    }

    [Fact]
    public async Task Send_CommandWithoutResponse_RunsHandler()
    {
        using var host = TestScope.Create();

        await host.Sender.Send(new Ping(), Token);

        host.Log.Entries.ShouldBe(["ping"]);
    }

    [Fact]
    public async Task Send_Query_ReturnsHandlerResponse()
    {
        using var host = TestScope.Create();

        var result = await host.Sender.Send(new GetGreeting("Hans"), Token);

        result.ShouldBe("Hello, Hans");
    }

    [Fact]
    public async Task Send_WithoutHandler_ThrowsInvalidOperation()
    {
        using var host = TestScope.Create();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => host.Sender.Send(new Unhandled(), Token));

        ex.Message.ShouldContain(nameof(Unhandled));
    }

    [Fact]
    public async Task Send_NullRequest_ThrowsArgumentNull()
    {
        using var host = TestScope.Create();

        await Should.ThrowAsync<ArgumentNullException>(() => host.Sender.Send<int>(null!, Token));
    }

    // CreateStream

    [Fact]
    public async Task CreateStream_YieldsAllItems()
    {
        using var host = TestScope.Create();
        var items = new List<int>();

        await foreach (var item in host.Sender.CreateStream(new CountTo(3), Token).ConfigureAwait(false))
        {
            items.Add(item);
        }

        items.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task CreateStream_Cancelled_StopsStream()
    {
        using var host = TestScope.Create();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var received = 0;

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in host.Sender.CreateStream(new CountTo(int.MaxValue), cts.Token).ConfigureAwait(false))
            {
                received = item;
                if (item == 2)
                {
                    await cts.CancelAsync().ConfigureAwait(false);
                }
            }
        });

        received.ShouldBe(2);
    }

    [Fact]
    public void CreateStream_WithoutHandler_ThrowsInvalidOperation()
    {
        using var host = TestScope.Create();

        var ex = Should.Throw<InvalidOperationException>(() => host.Sender.CreateStream(new UnhandledStream(), Token));

        ex.Message.ShouldContain(nameof(UnhandledStream));
    }

    [Fact]
    public void CreateStream_NullRequest_ThrowsArgumentNull()
    {
        using var host = TestScope.Create();

        Should.Throw<ArgumentNullException>(() => host.Sender.CreateStream<int>(null!, Token));
    }

    // Publish

    [Fact]
    public async Task Publish_RunsAllHandlersInOrder()
    {
        using var host = TestScope.Create();

        await host.Publisher.Publish(new OrderPlaced(7), Token);

        host.Log.Entries.ShouldBe(["first:7", "second:7"]);
    }

    [Fact]
    public async Task Publish_WithoutHandlers_Completes()
    {
        using var host = TestScope.Create(scanFixtures: false);

        await host.Publisher.Publish(new OrderPlaced(1), Token);

        host.Log.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Publish_NullNotification_ThrowsArgumentNull()
    {
        using var host = TestScope.Create();

        await Should.ThrowAsync<ArgumentNullException>(() => host.Publisher.Publish<OrderPlaced>(null!, Token));
    }

    [Fact]
    public async Task Publish_ConfiguredParallel_RunsHandlersConcurrently()
    {
        using var host = TestScope.Create(o => o.NotificationPublishStrategy = NotificationPublishStrategy.Parallel);

        await host.Publisher.Publish(new Rendezvoused(), Token);
    }

    [Fact]
    public async Task Publish_ExplicitParallel_OverridesSequentialDefault()
    {
        using var host = TestScope.Create();

        await host.Publisher.Publish(new Rendezvoused(), NotificationPublishStrategy.Parallel, Token);
    }

    [Fact]
    public async Task Publish_Sequential_DoesNotRunHandlersConcurrently()
    {
        using var host = TestScope.Create();

        // The first handler waits for the second, which never starts while the first is awaited.
        await Should.ThrowAsync<TimeoutException>(() => host.Publisher.Publish(new Rendezvoused(), Token));
    }

    [Fact]
    public async Task Publish_Parallel_RunsAllHandlersAndAggregatesFailures()
    {
        using var host = TestScope.Create();

        var ex = await Should.ThrowAsync<AggregateException>(() =>
            host.Publisher.Publish(new Failed(), NotificationPublishStrategy.Parallel, Token));

        ex.InnerExceptions.Count.ShouldBe(2);
        host.Log.Entries.ShouldBe(["third"]);
    }

    [Fact]
    public async Task Publish_Sequential_StopsAtFirstFailure()
    {
        using var host = TestScope.Create();

        await Should.ThrowAsync<InvalidOperationException>(() => host.Publisher.Publish(new Failed(), Token));

        host.Log.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Publish_UndefinedStrategy_ThrowsArgumentOutOfRange()
    {
        using var host = TestScope.Create();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            host.Publisher.Publish(new OrderPlaced(1), (NotificationPublishStrategy)42, Token));
    }
}
