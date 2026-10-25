using Dotnetstore.CQRS.Abstractions;

namespace Dotnetstore.CQRS.Tests.Fixtures;

internal sealed record CountTo(int Max) : IStreamRequest<int>;

internal sealed class CountToHandler : IStreamRequestHandler<CountTo, int>
{
    public async IAsyncEnumerable<int> Handle(CountTo request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 1; i <= request.Max; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return i;
            await Task.Yield();
        }
    }
}

internal sealed record UnhandledStream : IStreamRequest<int>;
