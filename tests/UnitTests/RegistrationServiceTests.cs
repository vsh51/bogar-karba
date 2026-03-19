using Application.Interfaces;
using Application.UseCases.Registration;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class RegistrationServiceTests
{
    private static readonly string[] SingleError = ["Password is too weak."];
    private static readonly string[] MultipleErrors = ["Error one.", "Error two."];

    private readonly Mock<IRegistrationUserRepository> _repositoryMock;
    private readonly Mock<ILogger<RegistrationService>> _loggerMock;
    private readonly RegistrationService _sut;

    public RegistrationServiceTests()
    {
        _repositoryMock = new Mock<IRegistrationUserRepository>();
        _loggerMock = new Mock<ILogger<RegistrationService>>();
        _sut = new RegistrationService(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidNewUser_ReturnsSuccessAndCallsCreate()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("test@example.com"))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.CreateUserAsync("John", "Doe", "test@example.com", "Password1", UserStatus.Active))
            .ReturnsAsync((true, Enumerable.Empty<string>()));

        var result = await _sut.RegisterAsync("John", "Doe", "test@example.com", "Password1");

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        _repositoryMock.Verify(
            r => r.CreateUserAsync("John", "Doe", "test@example.com", "Password1", UserStatus.Active),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyTaken_ReturnsFailureAndDoesNotCallCreate()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("existing@example.com"))
            .ReturnsAsync(true);

        var result = await _sut.RegisterAsync("John", "Doe", "existing@example.com", "Password1");

        Assert.False(result.Succeeded);
        Assert.Equal("Email is already taken.", result.ErrorMessage);
        _repositoryMock.Verify(
            r => r.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserStatus>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_IdentityCreationFails_ReturnsFailureWithErrorMessage()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("new@example.com"))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.CreateUserAsync("John", "Doe", "new@example.com", "weak", UserStatus.Active))
            .ReturnsAsync((false, SingleError));

        var result = await _sut.RegisterAsync("John", "Doe", "new@example.com", "weak");

        Assert.False(result.Succeeded);
        Assert.Contains("Password is too weak.", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterAsync_MultipleIdentityErrors_JoinsErrorMessages()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("new@example.com"))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.CreateUserAsync("John", "Doe", "new@example.com", "bad", UserStatus.Active))
            .ReturnsAsync((false, MultipleErrors));

        var result = await _sut.RegisterAsync("John", "Doe", "new@example.com", "bad");

        Assert.False(result.Succeeded);
        Assert.Contains("Error one.", result.ErrorMessage);
        Assert.Contains("Error two.", result.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task RegisterAsync_EmptyOrWhitespaceEmail_DelegatesToRepository(string email)
    {
        _repositoryMock.Setup(r => r.UserExistsAsync(email))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.CreateUserAsync("John", "Doe", email, "Password1", UserStatus.Active))
            .ReturnsAsync((true, Enumerable.Empty<string>()));

        var result = await _sut.RegisterAsync("John", "Doe", email, "Password1");

        Assert.True(result.Succeeded);
        _repositoryMock.Verify(r => r.CreateUserAsync("John", "Doe", email, "Password1", UserStatus.Active), Times.Once);
    }
}
