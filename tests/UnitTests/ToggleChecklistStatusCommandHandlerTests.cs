using Application.Common;
using Application.Interfaces;
using Application.UseCases.ToggleChecklistStatus;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class ToggleChecklistStatusCommandHandlerTests
{
    private readonly Mock<IChecklistRepository> _repositoryMock;
    private readonly Mock<IChecklistReadOnlyRepository> _readRepositoryMock;
    private readonly Mock<ILogger<ToggleChecklistStatusCommandHandler>> _loggerMock;
    private readonly ToggleChecklistStatusCommandHandler _handler;

    public ToggleChecklistStatusCommandHandlerTests()
    {
        _repositoryMock = new Mock<IChecklistRepository>();
        _readRepositoryMock = new Mock<IChecklistReadOnlyRepository>();
        _loggerMock = new Mock<ILogger<ToggleChecklistStatusCommandHandler>>();
        _handler = new ToggleChecklistStatusCommandHandler(
            _repositoryMock.Object,
            _readRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnSuccessWhenOwnerActivatesChecklist()
    {
        var checklistId = Guid.NewGuid();
        var userId = "user-123";
        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId))
                           .ReturnsAsync(new Checklist { Id = checklistId, UserId = userId, Status = ChecklistStatus.Draft });

        var result = await _handler.HandleAsync(
            new ToggleChecklistStatusCommand(checklistId, ChecklistStatus.Published, userId));

        Assert.True(result.Succeeded);
        _repositoryMock.Verify(r => r.UpdateStatusAsync(checklistId, ChecklistStatus.Published), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnSuccessWhenOwnerDeactivatesChecklist()
    {
        var checklistId = Guid.NewGuid();
        var userId = "user-123";
        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId))
                           .ReturnsAsync(new Checklist { Id = checklistId, UserId = userId, Status = ChecklistStatus.Published });

        var result = await _handler.HandleAsync(
            new ToggleChecklistStatusCommand(checklistId, ChecklistStatus.Draft, userId));

        Assert.True(result.Succeeded);
        _repositoryMock.Verify(r => r.UpdateStatusAsync(checklistId, ChecklistStatus.Draft), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnFailureWhenChecklistNotFound()
    {
        var checklistId = Guid.NewGuid();
        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId))
                           .ReturnsAsync((Checklist?)null);

        var result = await _handler.HandleAsync(
            new ToggleChecklistStatusCommand(checklistId, ChecklistStatus.Published, "user-123"));

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.ChecklistNotFound, result.ErrorMessage);
        _repositoryMock.Verify(r => r.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<ChecklistStatus>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnFailureWhenUserDoesNotOwnChecklist()
    {
        var checklistId = Guid.NewGuid();
        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId))
                           .ReturnsAsync(new Checklist { Id = checklistId, UserId = "owner-456" });

        var result = await _handler.HandleAsync(
            new ToggleChecklistStatusCommand(checklistId, ChecklistStatus.Published, "user-123"));

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.NotChecklistOwner, result.ErrorMessage);
        _repositoryMock.Verify(r => r.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<ChecklistStatus>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnSuccessWithoutOwnershipCheckWhenOwnerIdIsNull()
    {
        var checklistId = Guid.NewGuid();
        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId))
                           .ReturnsAsync(new Checklist { Id = checklistId, UserId = "any-user" });

        var result = await _handler.HandleAsync(
            new ToggleChecklistStatusCommand(checklistId, ChecklistStatus.Published));

        Assert.True(result.Succeeded);
        _repositoryMock.Verify(r => r.UpdateStatusAsync(checklistId, ChecklistStatus.Published), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncShouldLogInformation()
    {
        var checklistId = Guid.NewGuid();
        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId))
                           .ReturnsAsync(new Checklist { Id = checklistId, UserId = "user-123" });

        await _handler.HandleAsync(
            new ToggleChecklistStatusCommand(checklistId, ChecklistStatus.Published, "user-123"));

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
