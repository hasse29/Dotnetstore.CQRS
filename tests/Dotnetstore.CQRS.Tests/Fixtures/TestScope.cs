using Dotnetstore.CQRS.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.CQRS.Tests.Fixtures;

internal sealed class TestScope : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    private TestScope(ServiceProvider provider)
    {
        _provider = provider;
        _scope = provider.CreateScope();
    }

    public ISender Sender => _scope.ServiceProvider.GetRequiredService<ISender>();

    public IPublisher Publisher => _scope.ServiceProvider.GetRequiredService<IPublisher>();

    public CallLog Log => _scope.ServiceProvider.GetRequiredService<CallLog>();

    public static TestScope Create(Action<CqrsOptions>? configure = null, bool scanFixtures = true)
    {
        var services = new ServiceCollection();
        services.AddScoped<CallLog>();
        services.AddSingleton<Rendezvous>();
        services.AddCqrs(options =>
        {
            if (scanFixtures)
            {
                options.AddHandlersFromAssemblyContaining<CallLog>();
            }

            configure?.Invoke(options);
        });

        return new TestScope(services.BuildServiceProvider(validateScopes: true));
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }
}
