using Application.Interfaces;
using Application.UseCases.DeleteAuthorChecklist;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests;

public class DeleteAuthorChecklistCommandHandlerTests
{
    private readonly Mock<IChecklistRepository> _repositoryMock;
    private readonly Mock<ILogger<DeleteAuthorChecklistCommandHandler>> _loggerMock;
    private readonly DeleteAuthorChecklistCommandHandler _handler;

    public DeleteAuthorChecklistCommandHandlerTests()
    {
        _repositoryMock = new Mock<IChecklistRepository>();
        _loggerMock = new Mock<ILogger<DeleteAuthorChecklistCommandHandler>>();
        _handler = new DeleteAuthorChecklistCommandHandler(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnSuccessWhenUserOwnsChecklist()
    {
        var checklistId = Guid.NewGuid();
        var userId = "user-123";
        _repositoryMock.Setup(r => r.GetByIdAsync(checklistId))
                       .ReturnsAsync(new Checklist { Id = checklistId, UserId = userId });

        var result = await _handler.HandleAsync(new DeleteAuthorChecklistCommand(checklistId, userId));

        Assert.True(result.Succeeded);
        _repositoryMock.Verify(r => r.DeleteAsync(checklistId), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnFailureWhenChecklistNotFound()
    {
        var checklistId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(checklistId))
                       .ReturnsAsync((Checklist?)null);

        var result = await _handler.HandleAsync(new DeleteAuthorChecklistCommand(checklistId, "user-123"));

        Assert.False(result.Succeeded);
        Assert.Equal("Checklist not found.", result.ErrorMessage);
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnFailureWhenUserDoesNotOwnChecklist()
    {
        var checklistId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(checklistId))
                       .ReturnsAsync(new Checklist { Id = checklistId, UserId = "owner-456" });

        var result = await _handler.HandleAsync(new DeleteAuthorChecklistCommand(checklistId, "user-123"));

        Assert.False(result.Succeeded);
        Assert.Equal("You can only delete your own checklists.", result.ErrorMessage);
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnFailureWhenRepositoryThrows()
    {
        var checklistId = Guid.NewGuid();
        var userId = "user-123";
        _repositoryMock.Setup(r => r.GetByIdAsync(checklistId))
                       .ReturnsAsync(new Checklist { Id = checklistId, UserId = userId });
        _repositoryMock.Setup(r => r.DeleteAsync(checklistId))
                       .ThrowsAsync(new InvalidOperationException("DB Error"));

        var result = await _handler.HandleAsync(new DeleteAuthorChecklistCommand(checklistId, userId));

        Assert.False(result.Succeeded);
        Assert.Equal("Failed to delete checklist.", result.ErrorMessage);
    }
}
