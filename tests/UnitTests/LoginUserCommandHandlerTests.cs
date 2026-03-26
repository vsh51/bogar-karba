using Application.Enums;
using Application.Interfaces;
using Application.UseCases.Auth.LoginUser;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class LoginUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<ISignInService> _signInServiceMock;
    private readonly Mock<ILogger<LoginUserCommandHandler>> _loggerMock;
    private readonly LoginUserCommandHandler _sut;

    public LoginUserCommandHandlerTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _signInServiceMock = new Mock<ISignInService>();
        _loggerMock = new Mock<ILogger<LoginUserCommandHandler>>();

        _sut = new LoginUserCommandHandler(
            _repositoryMock.Object,
            _signInServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsyncValidActiveUserReturnsSuccess()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("user@test.com", UserLookupMode.ByEmail)).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.IsActiveAsync("user@test.com", UserLookupMode.ByEmail)).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.CheckPasswordAsync("user@test.com", "Pass123", UserLookupMode.ByEmail)).ReturnsAsync(true);

        var result = await _sut.HandleAsync(new LoginUserCommand("user@test.com", "Pass123"));

        Assert.True(result.Succeeded);
        _signInServiceMock.Verify(s => s.SignInAsync("user@test.com", UserLookupMode.ByEmail), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncUserNotFoundReturnsFailure()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("noone@test.com", UserLookupMode.ByEmail)).ReturnsAsync(false);

        var result = await _sut.HandleAsync(new LoginUserCommand("noone@test.com", "Pass123"));

        Assert.False(result.Succeeded);
        _signInServiceMock.Verify(s => s.SignInAsync(It.IsAny<string>(), It.IsAny<UserLookupMode>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsyncInactiveUserReturnsFailure()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("banned@test.com", UserLookupMode.ByEmail)).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.IsActiveAsync("banned@test.com", UserLookupMode.ByEmail)).ReturnsAsync(false);

        var result = await _sut.HandleAsync(new LoginUserCommand("banned@test.com", "Pass123"));

        Assert.False(result.Succeeded);
        Assert.Equal("Your account is blocked. Please contact administrator.", result.ErrorMessage);
        _repositoryMock.Verify(r => r.CheckPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserLookupMode>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsyncWrongPasswordReturnsFailure()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("user@test.com", UserLookupMode.ByEmail)).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.IsActiveAsync("user@test.com", UserLookupMode.ByEmail)).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.CheckPasswordAsync("user@test.com", "wrong", UserLookupMode.ByEmail)).ReturnsAsync(false);

        var result = await _sut.HandleAsync(new LoginUserCommand("user@test.com", "wrong"));

        Assert.False(result.Succeeded);
        _signInServiceMock.Verify(s => s.SignInAsync(It.IsAny<string>(), It.IsAny<UserLookupMode>()), Times.Never);
    }
}
