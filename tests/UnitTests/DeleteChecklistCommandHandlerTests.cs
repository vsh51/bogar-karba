using Application.Interfaces;
using Application.UseCases.DeleteChecklist;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests;

public class DeleteChecklistCommandHandlerTests
{
    private readonly Mock<IChecklistRepository> _repositoryMock;
    private readonly Mock<ILogger<DeleteChecklistCommandHandler>> _loggerMock;
    private readonly DeleteChecklistCommandHandler _handler;

    public DeleteChecklistCommandHandlerTests()
    {
        _repositoryMock = new Mock<IChecklistRepository>();
        _loggerMock = new Mock<ILogger<DeleteChecklistCommandHandler>>();
        _handler = new DeleteChecklistCommandHandler(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnSuccessWhenIdIsValid()
    {
        var checklistId = Guid.NewGuid();

        var result = await _handler.HandleAsync(new DeleteChecklistCommand(checklistId));

        Assert.True(result.Succeeded);
        _repositoryMock.Verify(repo => repo.DeleteAsync(checklistId), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnFailureWhenRepositoryFails()
    {
        var checklistId = Guid.NewGuid();
        _repositoryMock.Setup(repo => repo.DeleteAsync(checklistId))
                       .ThrowsAsync(new InvalidOperationException("DB Error"));

        var result = await _handler.HandleAsync(new DeleteChecklistCommand(checklistId));

        Assert.False(result.Succeeded);
        Assert.Contains(checklistId.ToString(), result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsyncShouldLogInformation()
    {
        var checklistId = Guid.NewGuid();

        await _handler.HandleAsync(new DeleteChecklistCommand(checklistId));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
