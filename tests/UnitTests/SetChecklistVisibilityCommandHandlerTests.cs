using Application.Common;
using Application.Interfaces;
using Application.UseCases.SetChecklistVisibility;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class SetChecklistVisibilityCommandHandlerTests
{
    private readonly Mock<IChecklistRepository> _repositoryMock;
    private readonly Mock<IChecklistReadOnlyRepository> _readRepositoryMock;
    private readonly SetChecklistVisibilityCommandHandler _handler;

    public SetChecklistVisibilityCommandHandlerTests()
    {
        _repositoryMock = new Mock<IChecklistRepository>();
        _readRepositoryMock = new Mock<IChecklistReadOnlyRepository>();
        var loggerMock = new Mock<ILogger<SetChecklistVisibilityCommandHandler>>();
        _handler = new SetChecklistVisibilityCommandHandler(
            _repositoryMock.Object,
            _readRepositoryMock.Object,
            loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsyncShouldSetPublicWhenOwnerUpdatesChecklist()
    {
        var checklistId = Guid.NewGuid();
        var userId = "user-123";
        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId))
            .ReturnsAsync(new Checklist { Id = checklistId, UserId = userId, IsPublic = false });

        var result = await _handler.HandleAsync(new SetChecklistVisibilityCommand(checklistId, true, userId));

        Assert.True(result.Succeeded);
        _repositoryMock.Verify(r => r.UpdateVisibilityAsync(checklistId, true), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnFailureWhenNotOwner()
    {
        var checklistId = Guid.NewGuid();
        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId))
            .ReturnsAsync(new Checklist { Id = checklistId, UserId = "owner-456" });

        var result = await _handler.HandleAsync(new SetChecklistVisibilityCommand(checklistId, false, "user-123"));

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.NotChecklistOwner, result.ErrorMessage);
        _repositoryMock.Verify(r => r.UpdateVisibilityAsync(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
    }
}
