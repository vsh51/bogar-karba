using Application.Interfaces;
using Application.UseCases.AdminAuth;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class AdminAuthServiceTests
{
    private readonly Mock<IAdminUserRepository> _repositoryMock;
    private readonly Mock<IAdminSignInService> _signInServiceMock;
    private readonly Mock<ILogger<AdminAuthService>> _loggerMock;
    private readonly AdminAuthService _sut;

    public AdminAuthServiceTests()
    {
        _repositoryMock = new Mock<IAdminUserRepository>();
        _signInServiceMock = new Mock<IAdminSignInService>();
        _loggerMock = new Mock<ILogger<AdminAuthService>>();

        _sut = new AdminAuthService(
            _repositoryMock.Object,
            _signInServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task LoginAsyncValidAdminReturnsSuccess()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("admin"))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetRolesAsync("admin"))
            .ReturnsAsync(new List<string> { "Admin" });
        _repositoryMock.Setup(r => r.CheckPasswordAsync("admin", "Admin123!"))
            .ReturnsAsync(true);

        var result = await _sut.LoginAsync("admin", "Admin123!");

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        _signInServiceMock.Verify(s => s.SignInAsync("admin"), Times.Once);
    }

    [Fact]
    public async Task LoginAsyncUserNotFoundReturnsFailure()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("nonexistent"))
            .ReturnsAsync(false);

        var result = await _sut.LoginAsync("nonexistent", "password");

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid username or password.", result.ErrorMessage);
        _signInServiceMock.Verify(s => s.SignInAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsyncUserIsNotAdminReturnsFailure()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("regularuser"))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetRolesAsync("regularuser"))
            .ReturnsAsync(new List<string> { "User" });

        var result = await _sut.LoginAsync("regularuser", "password");

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid username or password.", result.ErrorMessage);
        _signInServiceMock.Verify(s => s.SignInAsync(It.IsAny<string>()), Times.Never);
        _repositoryMock.Verify(r => r.CheckPasswordAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsyncWrongPasswordReturnsFailure()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("admin"))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetRolesAsync("admin"))
            .ReturnsAsync(new List<string> { "Admin" });
        _repositoryMock.Setup(r => r.CheckPasswordAsync("admin", "wrongpassword"))
            .ReturnsAsync(false);

        var result = await _sut.LoginAsync("admin", "wrongpassword");

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid username or password.", result.ErrorMessage);
        _signInServiceMock.Verify(s => s.SignInAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task LoginAsyncEmptyOrWhitespaceUserNameReturnsFailure(string userName)
    {
        _repositoryMock.Setup(r => r.UserExistsAsync(userName))
            .ReturnsAsync(false);

        var result = await _sut.LoginAsync(userName, "password");

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid username or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task LogoutAsyncCallsSignOutOnce()
    {
        await _sut.LogoutAsync();

        _signInServiceMock.Verify(s => s.SignOutAsync(), Times.Once);
    }

    [Fact]
    public async Task LoginAsyncUserHasNoRolesReturnsFailure()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("norolesuser"))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetRolesAsync("norolesuser"))
            .ReturnsAsync(new List<string>());

        var result = await _sut.LoginAsync("norolesuser", "password");

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid username or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsyncValidAdminDoesNotCallSignInBeforePasswordCheck()
    {
        var callOrder = new List<string>();

        _repositoryMock.Setup(r => r.UserExistsAsync("admin"))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetRolesAsync("admin"))
            .ReturnsAsync(new List<string> { "Admin" });
        _repositoryMock.Setup(r => r.CheckPasswordAsync("admin", "Admin123!"))
            .Callback(() => callOrder.Add("CheckPassword"))
            .ReturnsAsync(true);
        _signInServiceMock.Setup(s => s.SignInAsync("admin"))
            .Callback(() => callOrder.Add("SignIn"));

        await _sut.LoginAsync("admin", "Admin123!");

        Assert.Equal(2, callOrder.Count);
        Assert.Equal("CheckPassword", callOrder[0]);
        Assert.Equal("SignIn", callOrder[1]);
    }
}
