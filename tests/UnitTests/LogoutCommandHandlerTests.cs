using Application.Interfaces;
using Application.UseCases.Auth.Logout;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class LogoutCommandHandlerTests
{
    [Fact]
    public async Task HandleAsyncCallsSignOutOnceAndReturnsSuccess()
    {
        var signInServiceMock = new Mock<ISignInService>();
        var loggerMock = new Mock<ILogger<LogoutCommandHandler>>();
        var sut = new LogoutCommandHandler(signInServiceMock.Object, loggerMock.Object);

        var result = await sut.HandleAsync(new LogoutCommand(DateTime.UtcNow));

        Assert.True(result.Succeeded);
        signInServiceMock.Verify(s => s.SignOutAsync(), Times.Once);
    }
}
