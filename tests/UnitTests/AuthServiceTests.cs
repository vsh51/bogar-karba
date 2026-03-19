using Application.Interfaces;
using Application.UseCases.Auth;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class AuthServiceTests
{
    private readonly Mock<IAuthUserRepository> _repositoryMock;
    private readonly Mock<IAuthSignInService> _signInServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _repositoryMock = new Mock<IAuthUserRepository>();
        _signInServiceMock = new Mock<IAuthSignInService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _sut = new AuthService(
            _repositoryMock.Object,
            _signInServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task LoginAsyncValidActiveUserReturnsTrue()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("user@test.com")).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.IsActiveAsync("user@test.com")).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.CheckPasswordAsync("user@test.com", "Pass123")).ReturnsAsync(true);

        var result = await _sut.LoginAsync("user@test.com", "Pass123");

        Assert.True(result);
        _signInServiceMock.Verify(s => s.SignInAsync("user@test.com"), Times.Once);
    }

    [Fact]
    public async Task LoginAsyncUserNotFoundReturnsFalse()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("noone@test.com")).ReturnsAsync(false);

        var result = await _sut.LoginAsync("noone@test.com", "Pass123");

        Assert.False(result);
        _signInServiceMock.Verify(s => s.SignInAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsyncInactiveUserReturnsFalse()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("banned@test.com")).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.IsActiveAsync("banned@test.com")).ReturnsAsync(false);

        var result = await _sut.LoginAsync("banned@test.com", "Pass123");

        Assert.False(result);
        _repositoryMock.Verify(r => r.CheckPasswordAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsyncWrongPasswordReturnsFalse()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("user@test.com")).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.IsActiveAsync("user@test.com")).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.CheckPasswordAsync("user@test.com", "wrong")).ReturnsAsync(false);

        var result = await _sut.LoginAsync("user@test.com", "wrong");

        Assert.False(result);
        _signInServiceMock.Verify(s => s.SignInAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsyncCallsSignOut()
    {
        await _sut.LogoutAsync();

        _signInServiceMock.Verify(s => s.SignOutAsync(), Times.Once);
    }
}
