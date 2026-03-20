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
    public async Task HandleAsyncValidAdminReturnsSuccess()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("admin", UserLookupMode.ByUserName))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetRolesAsync("admin", UserLookupMode.ByUserName))
            .ReturnsAsync(new List<string> { "Admin" });
        _repositoryMock.Setup(r => r.CheckPasswordAsync("admin", "Admin123!", UserLookupMode.ByUserName))
            .ReturnsAsync(true);

        var result = await _sut.HandleAsync(new LoginAdminCommand("admin", "Admin123!"));

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        _signInServiceMock.Verify(s => s.SignInAsync("admin", UserLookupMode.ByUserName), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncUserNotFoundReturnsFailure()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("nonexistent", UserLookupMode.ByUserName))
            .ReturnsAsync(false);

        var result = await _sut.HandleAsync(new LoginAdminCommand("nonexistent", "password"));

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid username or password.", result.ErrorMessage);
        _signInServiceMock.Verify(s => s.SignInAsync(It.IsAny<string>(), It.IsAny<UserLookupMode>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsyncUserIsNotAdminReturnsFailure()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("regularuser", UserLookupMode.ByUserName))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetRolesAsync("regularuser", UserLookupMode.ByUserName))
            .ReturnsAsync(new List<string> { "User" });

        var result = await _sut.HandleAsync(new LoginAdminCommand("regularuser", "password"));

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid username or password.", result.ErrorMessage);
        _signInServiceMock.Verify(s => s.SignInAsync(It.IsAny<string>(), It.IsAny<UserLookupMode>()), Times.Never);
        _repositoryMock.Verify(r => r.CheckPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserLookupMode>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsyncWrongPasswordReturnsFailure()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("admin", UserLookupMode.ByUserName))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetRolesAsync("admin", UserLookupMode.ByUserName))
            .ReturnsAsync(new List<string> { "Admin" });
        _repositoryMock.Setup(r => r.CheckPasswordAsync("admin", "wrongpassword", UserLookupMode.ByUserName))
            .ReturnsAsync(false);

        var result = await _sut.HandleAsync(new LoginAdminCommand("admin", "wrongpassword"));

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid username or password.", result.ErrorMessage);
        _signInServiceMock.Verify(s => s.SignInAsync(It.IsAny<string>(), It.IsAny<UserLookupMode>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task HandleAsyncEmptyOrWhitespaceUserNameReturnsFailure(string userName)
    {
        _repositoryMock.Setup(r => r.UserExistsAsync(userName, UserLookupMode.ByUserName))
            .ReturnsAsync(false);

        var result = await _sut.HandleAsync(new LoginAdminCommand(userName, "password"));

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid username or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsyncUserHasNoRolesReturnsFailure()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("norolesuser", UserLookupMode.ByUserName))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetRolesAsync("norolesuser", UserLookupMode.ByUserName))
            .ReturnsAsync(new List<string>());

        var result = await _sut.HandleAsync(new LoginAdminCommand("norolesuser", "password"));

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid username or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsyncValidAdminDoesNotCallSignInBeforePasswordCheck()
    {
        var callOrder = new List<string>();

        _repositoryMock.Setup(r => r.UserExistsAsync("admin", UserLookupMode.ByUserName))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetRolesAsync("admin", UserLookupMode.ByUserName))
            .ReturnsAsync(new List<string> { "Admin" });
        _repositoryMock.Setup(r => r.CheckPasswordAsync("admin", "Admin123!", UserLookupMode.ByUserName))
            .Callback(() => callOrder.Add("CheckPassword"))
            .ReturnsAsync(true);
        _signInServiceMock.Setup(s => s.SignInAsync("admin", UserLookupMode.ByUserName))
            .Callback(() => callOrder.Add("SignIn"));

        await _sut.HandleAsync(new LoginAdminCommand("admin", "Admin123!"));

        Assert.Equal(2, callOrder.Count);
        Assert.Equal("CheckPassword", callOrder[0]);
        Assert.Equal("SignIn", callOrder[1]);
    }
}
