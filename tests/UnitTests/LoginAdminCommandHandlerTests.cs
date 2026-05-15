using Application.Enums;
using Application.Interfaces;
using Application.UseCases.Auth.LoginAdmin;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class LoginAdminCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<ISignInService> _signInServiceMock;
    private readonly Mock<ILogger<LoginAdminCommandHandler>> _loggerMock;
    private readonly LoginAdminCommandHandler _sut;

    public LoginAdminCommandHandlerTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _signInServiceMock = new Mock<ISignInService>();
        _loggerMock = new Mock<ILogger<LoginAdminCommandHandler>>();

        _sut = new LoginAdminCommandHandler(
            _repositoryMock.Object,
            _signInServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsyncValidAdminWithUsernameReturnsSuccess()
    {
        var identifier = "admin";
        _repositoryMock.Setup(r => r.UserExistsAsync(identifier, UserLookupMode.ByUserName))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetRolesAsync(identifier, UserLookupMode.ByUserName))
            .ReturnsAsync(new List<string> { "Admin" });
        _repositoryMock.Setup(r => r.CheckPasswordAsync(identifier, "Admin123!", UserLookupMode.ByUserName))
            .ReturnsAsync(true);

        var result = await _sut.HandleAsync(new LoginAdminCommand(identifier, "Admin123!"));

        Assert.True(result.Succeeded);
        _signInServiceMock.Verify(s => s.SignInAsync(identifier, UserLookupMode.ByUserName), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncValidAdminWithEmailReturnsSuccess()
    {
        var identifier = "admin@test.com";
        _repositoryMock.Setup(r => r.UserExistsAsync(identifier, UserLookupMode.ByEmail))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetRolesAsync(identifier, UserLookupMode.ByEmail))
            .ReturnsAsync(new List<string> { "Admin" });
        _repositoryMock.Setup(r => r.CheckPasswordAsync(identifier, "Admin123!", UserLookupMode.ByEmail))
            .ReturnsAsync(true);

        var result = await _sut.HandleAsync(new LoginAdminCommand(identifier, "Admin123!"));

        Assert.True(result.Succeeded);
        _signInServiceMock.Verify(s => s.SignInAsync(identifier, UserLookupMode.ByEmail), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncUserNotFoundReturnsFailure()
    {
        var identifier = "nonexistent";
        _repositoryMock.Setup(r => r.UserExistsAsync(identifier, UserLookupMode.ByUserName))
            .ReturnsAsync(false);

        var result = await _sut.HandleAsync(new LoginAdminCommand(identifier, "password"));

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid login or password.", result.ErrorMessage);
        _signInServiceMock.Verify(s => s.SignInAsync(It.IsAny<string>(), It.IsAny<UserLookupMode>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsyncUserIsNotAdminReturnsFailure()
    {
        var identifier = "regularuser";
        _repositoryMock.Setup(r => r.UserExistsAsync(identifier, UserLookupMode.ByUserName))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetRolesAsync(identifier, UserLookupMode.ByUserName))
            .ReturnsAsync(new List<string> { "User" });

        var result = await _sut.HandleAsync(new LoginAdminCommand(identifier, "password"));

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid login or password.", result.ErrorMessage);
        _signInServiceMock.Verify(s => s.SignInAsync(It.IsAny<string>(), It.IsAny<UserLookupMode>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsyncWrongPasswordReturnsFailure()
    {
        var identifier = "admin";
        _repositoryMock.Setup(r => r.UserExistsAsync(identifier, UserLookupMode.ByUserName))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetRolesAsync(identifier, UserLookupMode.ByUserName))
            .ReturnsAsync(new List<string> { "Admin" });
        _repositoryMock.Setup(r => r.CheckPasswordAsync(identifier, "wrongpassword", UserLookupMode.ByUserName))
            .ReturnsAsync(false);

        var result = await _sut.HandleAsync(new LoginAdminCommand(identifier, "wrongpassword"));

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid login or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsyncUserHasNoRolesReturnsFailure()
    {
        var identifier = "norolesuser";
        _repositoryMock.Setup(r => r.UserExistsAsync(identifier, UserLookupMode.ByUserName))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetRolesAsync(identifier, UserLookupMode.ByUserName))
            .ReturnsAsync(new List<string>());

        var result = await _sut.HandleAsync(new LoginAdminCommand(identifier, "password"));

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid login or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsyncValidAdminDoesNotCallSignInBeforePasswordCheck()
    {
        var identifier = "admin";
        var callOrder = new List<string>();

        _repositoryMock.Setup(r => r.UserExistsAsync(identifier, UserLookupMode.ByUserName))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetRolesAsync(identifier, UserLookupMode.ByUserName))
            .ReturnsAsync(new List<string> { "Admin" });
        _repositoryMock.Setup(r => r.CheckPasswordAsync(identifier, "Admin123!", UserLookupMode.ByUserName))
            .Callback(() => callOrder.Add("CheckPassword"))
            .ReturnsAsync(true);
        _signInServiceMock.Setup(s => s.SignInAsync(identifier, UserLookupMode.ByUserName))
            .Callback(() => callOrder.Add("SignIn"));

        await _sut.HandleAsync(new LoginAdminCommand(identifier, "Admin123!"));

        Assert.Equal(2, callOrder.Count);
        Assert.Equal("CheckPassword", callOrder[0]);
        Assert.Equal("SignIn", callOrder[1]);
    }
}
