using Dotnetstore.CQRS.Abstractions;
using Dotnetstore.CQRS.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.CQRS.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCqrs_RegistersSenderAndPublisher()
    {
        var services = new ServiceCollection().AddCqrs(_ => { });
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetService<ISender>().ShouldNotBeNull();
        scope.ServiceProvider.GetService<IPublisher>().ShouldNotBeNull();
    }

    [Fact]
    public void AddCqrs_ScannedAssembly_RegistersHandlersByContract()
    {
        var services = new ServiceCollection().AddScoped<CallLog>().AddCqrs(o => o.AddHandlersFromAssemblyContaining<CallLog>());
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IRequestHandler<CreateOrder, int>>().ShouldBeOfType<CreateOrderHandler>();
        provider.GetRequiredService<IStreamRequestHandler<CountTo, int>>().ShouldBeOfType<CountToHandler>();
        provider.GetServices<INotificationHandler<OrderPlaced>>().Count().ShouldBe(2);
        provider.GetRequiredService<IRequestPreProcessor<Tracked>>().ShouldBeOfType<TrackedPreProcessor>();
    }

    [Fact]
    public void AddCqrs_NoAssemblies_RegistersNoHandlers()
    {
        var services = new ServiceCollection().AddCqrs(_ => { });
        using var provider = services.BuildServiceProvider();

        provider.GetService<IRequestHandler<CreateOrder, int>>().ShouldBeNull();
    }

    [Fact]
    public void AddCqrs_NullConfigure_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new ServiceCollection().AddCqrs(null!));
    }
}
