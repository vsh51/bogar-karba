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
    public async Task HandleAsyncValidActiveUserWithEmailReturnsSuccess()
    {
        var identifier = "user@test.com";
        _repositoryMock.Setup(r => r.UserExistsAsync(identifier, UserLookupMode.ByEmail)).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.IsActiveAsync(identifier, UserLookupMode.ByEmail)).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.CheckPasswordAsync(identifier, "Pass123", UserLookupMode.ByEmail)).ReturnsAsync(true);

        var result = await _sut.HandleAsync(new LoginUserCommand(identifier, "Pass123"));

        Assert.True(result.Succeeded);
        _signInServiceMock.Verify(s => s.SignInAsync(identifier, UserLookupMode.ByEmail), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncValidActiveUserWithUsernameReturnsSuccess()
    {
        var identifier = "username123";
        _repositoryMock.Setup(r => r.UserExistsAsync(identifier, UserLookupMode.ByUserName)).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.IsActiveAsync(identifier, UserLookupMode.ByUserName)).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.CheckPasswordAsync(identifier, "Pass123", UserLookupMode.ByUserName)).ReturnsAsync(true);

        var result = await _sut.HandleAsync(new LoginUserCommand(identifier, "Pass123"));

        Assert.True(result.Succeeded);
        _signInServiceMock.Verify(s => s.SignInAsync(identifier, UserLookupMode.ByUserName), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncUserNotFoundReturnsFailure()
    {
        var identifier = "noone@test.com";
        _repositoryMock.Setup(r => r.UserExistsAsync(identifier, UserLookupMode.ByEmail)).ReturnsAsync(false);

        var result = await _sut.HandleAsync(new LoginUserCommand(identifier, "Pass123"));

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid login or password.", result.ErrorMessage);
        _signInServiceMock.Verify(s => s.SignInAsync(It.IsAny<string>(), It.IsAny<UserLookupMode>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsyncInactiveUserReturnsFailure()
    {
        var identifier = "banned@test.com";
        _repositoryMock.Setup(r => r.UserExistsAsync(identifier, UserLookupMode.ByEmail)).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.IsActiveAsync(identifier, UserLookupMode.ByEmail)).ReturnsAsync(false);

        var result = await _sut.HandleAsync(new LoginUserCommand(identifier, "Pass123"));

        Assert.False(result.Succeeded);
        Assert.Equal("Your account is blocked.", result.ErrorMessage);
        _repositoryMock.Verify(r => r.CheckPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserLookupMode>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsyncWrongPasswordReturnsFailure()
    {
        var identifier = "user@test.com";
        _repositoryMock.Setup(r => r.UserExistsAsync(identifier, UserLookupMode.ByEmail)).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.IsActiveAsync(identifier, UserLookupMode.ByEmail)).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.CheckPasswordAsync(identifier, "wrong", UserLookupMode.ByEmail)).ReturnsAsync(false);

        var result = await _sut.HandleAsync(new LoginUserCommand(identifier, "wrong"));

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid login or password.", result.ErrorMessage);
        _signInServiceMock.Verify(s => s.SignInAsync(It.IsAny<string>(), It.IsAny<UserLookupMode>()), Times.Never);
    }
}
