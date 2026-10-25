using Dotnetstore.CQRS.Abstractions;
using Dotnetstore.CQRS.Tests.Fixtures;

namespace Dotnetstore.CQRS.Tests.Extensions;

public class CqrsOptionsTests
{
    [Fact]
    public void NotificationPublishStrategy_DefaultsToSequential()
    {
        new CqrsOptions().NotificationPublishStrategy.ShouldBe(NotificationPublishStrategy.Sequential);
    }

    [Fact]
    public void AddOpenBehavior_ClosedType_Throws()
    {
        var options = new CqrsOptions();

        Should.Throw<ArgumentException>(() => options.AddOpenBehavior(typeof(string)));
    }

    [Fact]
    public void AddOpenBehavior_OpenBehavior_Chains()
    {
        var options = new CqrsOptions();

        options.AddOpenBehavior(typeof(NamedBehavior<,>)).ShouldBeSameAs(options);
    }

    [Fact]
    public void AddOpenPreProcessor_WrongType_Throws()
    {
        var options = new CqrsOptions();

        Should.Throw<ArgumentException>(() => options.AddOpenPreProcessor(typeof(string)));
        Should.Throw<ArgumentException>(() => options.AddOpenPreProcessor(typeof(OpenPostProcessor<,>)));
    }

    [Fact]
    public void AddOpenPostProcessor_WrongType_Throws()
    {
        var options = new CqrsOptions();

        Should.Throw<ArgumentException>(() => options.AddOpenPostProcessor(typeof(string)));
        Should.Throw<ArgumentException>(() => options.AddOpenPostProcessor(typeof(OpenPreProcessor<>)));
    }

    [Fact]
    public void AddHandlersFromAssembly_NullAssembly_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new CqrsOptions().AddHandlersFromAssembly(null!));
    }
}
