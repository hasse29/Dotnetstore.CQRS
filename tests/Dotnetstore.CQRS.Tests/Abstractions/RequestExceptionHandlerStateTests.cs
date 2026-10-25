using Dotnetstore.CQRS.Abstractions;

namespace Dotnetstore.CQRS.Tests.Abstractions;

public class RequestExceptionHandlerStateTests
{
    [Fact]
    public void New_IsNotHandled()
    {
        var state = new RequestExceptionHandlerState<string>();

        state.Handled.ShouldBeFalse();
        state.Response.ShouldBeNull();
    }

    [Fact]
    public void SetHandled_MarksHandledAndStoresResponse()
    {
        var state = new RequestExceptionHandlerState<string>();

        state.SetHandled("recovered");

        state.Handled.ShouldBeTrue();
        state.Response.ShouldBe("recovered");
    }
}
