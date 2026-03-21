using Application.Enums;
using Application.Interfaces;
using Application.UseCases.Auth.RegisterUser;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class RegisterUserCommandHandlerTests
{
    private static readonly string[] SingleError = ["Password is too weak."];
    private static readonly string[] MultipleErrors = ["Error one.", "Error two."];

    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<ILogger<RegisterUserCommandHandler>> _loggerMock;
    private readonly RegisterUserCommandHandler _sut;

    public RegisterUserCommandHandlerTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<RegisterUserCommandHandler>>();
        _sut = new RegisterUserCommandHandler(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsyncValidNewUserReturnsSuccessAndCallsCreate()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("test@example.com", UserLookupMode.ByEmail))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.CreateUserAsync("John", "Doe", "test@example.com", "Password1", UserStatus.Active))
            .ReturnsAsync((true, Enumerable.Empty<string>()));

        var result = await _sut.HandleAsync(
            new RegisterUserCommand("John", "Doe", "test@example.com", "Password1"));

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        _repositoryMock.Verify(
            r => r.CreateUserAsync("John", "Doe", "test@example.com", "Password1", UserStatus.Active),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsyncEmailAlreadyTakenReturnsFailureAndDoesNotCallCreate()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("existing@example.com", UserLookupMode.ByEmail))
            .ReturnsAsync(true);

        var result = await _sut.HandleAsync(
            new RegisterUserCommand("John", "Doe", "existing@example.com", "Password1"));

        Assert.False(result.Succeeded);
        Assert.Equal("Email is already taken.", result.ErrorMessage);
        _repositoryMock.Verify(
            r => r.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserStatus>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsyncIdentityCreationFailsReturnsFailureWithErrorMessage()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("new@example.com", UserLookupMode.ByEmail))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.CreateUserAsync("John", "Doe", "new@example.com", "weak", UserStatus.Active))
            .ReturnsAsync((false, SingleError));

        var result = await _sut.HandleAsync(
            new RegisterUserCommand("John", "Doe", "new@example.com", "weak"));

        Assert.False(result.Succeeded);
        Assert.Contains("Password is too weak.", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsyncMultipleIdentityErrorsJoinsErrorMessages()
    {
        _repositoryMock.Setup(r => r.UserExistsAsync("new@example.com", UserLookupMode.ByEmail))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.CreateUserAsync("John", "Doe", "new@example.com", "bad", UserStatus.Active))
            .ReturnsAsync((false, MultipleErrors));

        var result = await _sut.HandleAsync(
            new RegisterUserCommand("John", "Doe", "new@example.com", "bad"));

        Assert.False(result.Succeeded);
        Assert.Contains("Error one.", result.ErrorMessage);
        Assert.Contains("Error two.", result.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task HandleAsyncEmptyOrWhitespaceEmailDelegatesToRepository(string email)
    {
        _repositoryMock.Setup(r => r.UserExistsAsync(email, UserLookupMode.ByEmail))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.CreateUserAsync("John", "Doe", email, "Password1", UserStatus.Active))
            .ReturnsAsync((true, Enumerable.Empty<string>()));

        var result = await _sut.HandleAsync(
            new RegisterUserCommand("John", "Doe", email, "Password1"));

        Assert.True(result.Succeeded);
        _repositoryMock.Verify(r => r.CreateUserAsync("John", "Doe", email, "Password1", UserStatus.Active), Times.Once);
    }
}
