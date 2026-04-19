using Application.Common;
using Application.Interfaces;
using Application.UseCases.BanUser;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class BanUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<ILogger<BanUserCommandHandler>> _loggerMock;
    private readonly BanUserCommandHandler _handler;

    public BanUserCommandHandlerTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<BanUserCommandHandler>>();
        _handler = new BanUserCommandHandler(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnSuccessWhenUserExists()
    {
        _repositoryMock.Setup(repo => repo.BanUserAsync("user-id"))
            .ReturnsAsync(true);

        var result = await _handler.HandleAsync(new BanUserCommand("user-id"));

        Assert.True(result.Succeeded);
        _repositoryMock.Verify(repo => repo.BanUserAsync("user-id"), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnFailureWhenUserNotFound()
    {
        _repositoryMock.Setup(repo => repo.BanUserAsync("missing-user"))
            .ReturnsAsync(false);

        var result = await _handler.HandleAsync(new BanUserCommand("missing-user"));

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.UserNotFound, result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsyncShouldThrowWhenRepositoryThrows()
    {
        _repositoryMock.Setup(repo => repo.BanUserAsync("user-id"))
            .ThrowsAsync(new InvalidOperationException("DB Error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.HandleAsync(new BanUserCommand("user-id")));
    }
}
