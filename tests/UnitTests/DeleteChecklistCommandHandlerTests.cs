using Application.Interfaces;
using Application.UseCases.DeleteChecklist;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests;

public class DeleteChecklistCommandHandlerTests
{
    private readonly Mock<IChecklistRepository> _repositoryMock;
    private readonly Mock<IChecklistReadOnlyRepository> _readRepositoryMock;
    private readonly Mock<ILogger<DeleteChecklistCommandHandler>> _loggerMock;
    private readonly DeleteChecklistCommandHandler _handler;

    public DeleteChecklistCommandHandlerTests()
    {
        _repositoryMock = new Mock<IChecklistRepository>();
        _readRepositoryMock = new Mock<IChecklistReadOnlyRepository>();
        _loggerMock = new Mock<ILogger<DeleteChecklistCommandHandler>>();
        _handler = new DeleteChecklistCommandHandler(
            _repositoryMock.Object,
            _readRepositoryMock.Object,
            _loggerMock.Object);
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
    public async Task HandleAsyncShouldThrowWhenRepositoryFails()
    {
        var checklistId = Guid.NewGuid();
        _repositoryMock.Setup(repo => repo.DeleteAsync(checklistId))
                       .ThrowsAsync(new InvalidOperationException("DB Error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.HandleAsync(new DeleteChecklistCommand(checklistId)));
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

    [Fact]
    public async Task HandleAsyncShouldNotCheckOwnershipWhenOwnerIdIsNull()
    {
        var checklistId = Guid.NewGuid();

        await _handler.HandleAsync(new DeleteChecklistCommand(checklistId));

        _readRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _repositoryMock.Verify(r => r.DeleteAsync(checklistId), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnSuccessWhenUserOwnsChecklist()
    {
        var checklistId = Guid.NewGuid();
        var userId = "user-123";
        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId))
                           .ReturnsAsync(new Checklist { Id = checklistId, UserId = userId });

        var result = await _handler.HandleAsync(new DeleteChecklistCommand(checklistId, userId));

        Assert.True(result.Succeeded);
        _repositoryMock.Verify(repo => repo.DeleteAsync(checklistId), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnFailureWhenChecklistNotFound()
    {
        var checklistId = Guid.NewGuid();
        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId))
                           .ReturnsAsync((Checklist?)null);

        var result = await _handler.HandleAsync(new DeleteChecklistCommand(checklistId, "user-123"));

        Assert.False(result.Succeeded);
        Assert.Equal("Checklist not found.", result.ErrorMessage);
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnFailureWhenUserDoesNotOwnChecklist()
    {
        var checklistId = Guid.NewGuid();
        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId))
                           .ReturnsAsync(new Checklist { Id = checklistId, UserId = "owner-456" });

        var result = await _handler.HandleAsync(new DeleteChecklistCommand(checklistId, "user-123"));

        Assert.False(result.Succeeded);
        Assert.Equal("You can only delete your own checklists.", result.ErrorMessage);
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }
}
