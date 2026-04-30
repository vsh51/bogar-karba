using Application.Common;
using Application.Interfaces;
using Application.UseCases.SetChecklistEmbeddable;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class SetChecklistEmbeddableCommandHandlerTests
{
    private readonly Mock<IChecklistRepository> _repositoryMock;
    private readonly Mock<IChecklistReadOnlyRepository> _readRepositoryMock;
    private readonly SetChecklistEmbeddableCommandHandler _handler;

    public SetChecklistEmbeddableCommandHandlerTests()
    {
        _repositoryMock = new Mock<IChecklistRepository>();
        _readRepositoryMock = new Mock<IChecklistReadOnlyRepository>();
        var loggerMock = new Mock<ILogger<SetChecklistEmbeddableCommandHandler>>();
        _handler = new SetChecklistEmbeddableCommandHandler(
            _repositoryMock.Object,
            _readRepositoryMock.Object,
            loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsyncShouldEnableEmbedWhenOwnerUpdatesChecklist()
    {
        var checklistId = Guid.NewGuid();
        var userId = "user-123";
        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId))
            .ReturnsAsync(new Checklist { Id = checklistId, UserId = userId, IsEmbeddable = false });

        var result = await _handler.HandleAsync(new SetChecklistEmbeddableCommand(checklistId, true, userId));

        Assert.True(result.Succeeded);
        _repositoryMock.Verify(r => r.UpdateEmbeddableAsync(checklistId, true), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncShouldDisableEmbedWhenOwnerUpdatesChecklist()
    {
        var checklistId = Guid.NewGuid();
        var userId = "user-123";
        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId))
            .ReturnsAsync(new Checklist { Id = checklistId, UserId = userId, IsEmbeddable = true });

        var result = await _handler.HandleAsync(new SetChecklistEmbeddableCommand(checklistId, false, userId));

        Assert.True(result.Succeeded);
        _repositoryMock.Verify(r => r.UpdateEmbeddableAsync(checklistId, false), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnFailureWhenNotOwner()
    {
        var checklistId = Guid.NewGuid();
        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId))
            .ReturnsAsync(new Checklist { Id = checklistId, UserId = "owner-456" });

        var result = await _handler.HandleAsync(new SetChecklistEmbeddableCommand(checklistId, false, "user-123"));

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.NotChecklistOwner, result.ErrorMessage);
        _repositoryMock.Verify(r => r.UpdateEmbeddableAsync(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnFailureWhenChecklistNotFound()
    {
        var checklistId = Guid.NewGuid();
        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId))
            .ReturnsAsync((Checklist?)null);

        var result = await _handler.HandleAsync(new SetChecklistEmbeddableCommand(checklistId, true, "user-123"));

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.ChecklistNotFound, result.ErrorMessage);
        _repositoryMock.Verify(r => r.UpdateEmbeddableAsync(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
    }
}
