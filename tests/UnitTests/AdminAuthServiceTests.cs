using Application.Interfaces;
using Application.UseCases.AdminAuth;
using Domain.Entities;
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
        var user = new User { UserName = "admin" };
        _repositoryMock.Setup(r => r.GetByUserNameAsync("admin"))
            .ReturnsAsync(user);
        _repositoryMock.Setup(r => r.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Admin" });
        _repositoryMock.Setup(r => r.CheckPasswordAsync(user, "Admin123!"))
            .ReturnsAsync(true);

        var result = await _sut.LoginAsync("admin", "Admin123!");

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        _signInServiceMock.Verify(s => s.SignInAsync(user), Times.Once);
    }

    [Fact]
    public async Task LoginAsyncUserNotFoundReturnsFailure()
    {
        _repositoryMock.Setup(r => r.GetByUserNameAsync("nonexistent"))
            .ReturnsAsync((User?)null);

        var result = await _sut.LoginAsync("nonexistent", "password");

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid username or password.", result.ErrorMessage);
        _signInServiceMock.Verify(s => s.SignInAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsyncUserIsNotAdminReturnsFailure()
    {
        var user = new User { UserName = "regularuser" };
        _repositoryMock.Setup(r => r.GetByUserNameAsync("regularuser"))
            .ReturnsAsync(user);
        _repositoryMock.Setup(r => r.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        var result = await _sut.LoginAsync("regularuser", "password");

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid username or password.", result.ErrorMessage);
        _signInServiceMock.Verify(s => s.SignInAsync(It.IsAny<User>()), Times.Never);
        _repositoryMock.Verify(r => r.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsyncWrongPasswordReturnsFailure()
    {
        var user = new User { UserName = "admin" };
        _repositoryMock.Setup(r => r.GetByUserNameAsync("admin"))
            .ReturnsAsync(user);
        _repositoryMock.Setup(r => r.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Admin" });
        _repositoryMock.Setup(r => r.CheckPasswordAsync(user, "wrongpassword"))
            .ReturnsAsync(false);

        var result = await _sut.LoginAsync("admin", "wrongpassword");

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid username or password.", result.ErrorMessage);
        _signInServiceMock.Verify(s => s.SignInAsync(It.IsAny<User>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task LoginAsyncEmptyOrWhitespaceUserNameReturnsFailure(string userName)
    {
        _repositoryMock.Setup(r => r.GetByUserNameAsync(userName))
            .ReturnsAsync((User?)null);

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
        var user = new User { UserName = "norolesuser" };
        _repositoryMock.Setup(r => r.GetByUserNameAsync("norolesuser"))
            .ReturnsAsync(user);
        _repositoryMock.Setup(r => r.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        var result = await _sut.LoginAsync("norolesuser", "password");

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid username or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsyncValidAdminDoesNotCallSignInBeforePasswordCheck()
    {
        var user = new User { UserName = "admin" };
        var callOrder = new List<string>();

        _repositoryMock.Setup(r => r.GetByUserNameAsync("admin"))
            .ReturnsAsync(user);
        _repositoryMock.Setup(r => r.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Admin" });
        _repositoryMock.Setup(r => r.CheckPasswordAsync(user, "Admin123!"))
            .Callback(() => callOrder.Add("CheckPassword"))
            .ReturnsAsync(true);
        _signInServiceMock.Setup(s => s.SignInAsync(user))
            .Callback(() => callOrder.Add("SignIn"));

        await _sut.LoginAsync("admin", "Admin123!");

        Assert.Equal(2, callOrder.Count);
        Assert.Equal("CheckPassword", callOrder[0]);
        Assert.Equal("SignIn", callOrder[1]);
    }
}
