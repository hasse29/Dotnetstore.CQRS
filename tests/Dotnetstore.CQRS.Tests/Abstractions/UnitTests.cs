using Dotnetstore.CQRS.Abstractions;

namespace Dotnetstore.CQRS.Tests.Abstractions;

public class UnitTests
{
    [Fact]
    public void Value_EqualsDefault()
    {
        Unit.Value.ShouldBe(default);
    }

    [Fact]
    public async Task Task_IsCompletedAndReturnsUnit()
    {
        Unit.Task.IsCompletedSuccessfully.ShouldBeTrue();

        (await Unit.Task.ConfigureAwait(true)).ShouldBe(Unit.Value);
    }
}
