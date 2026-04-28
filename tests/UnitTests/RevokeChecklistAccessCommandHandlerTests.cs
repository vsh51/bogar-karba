using Application.Common;
using Application.Interfaces;
using Application.UseCases.RevokeChecklistAccess;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class RevokeChecklistAccessCommandHandlerTests
{
    private const string OwnerId = "owner-123";
    private const string TargetUserId = "target-456";

    private readonly Mock<IChecklistReadOnlyRepository> _readRepositoryMock;
    private readonly Mock<IChecklistRepository> _repositoryMock;
    private readonly RevokeChecklistAccessCommandHandler _handler;

    public RevokeChecklistAccessCommandHandlerTests()
    {
        _readRepositoryMock = new Mock<IChecklistReadOnlyRepository>();
        _repositoryMock = new Mock<IChecklistRepository>();
        _handler = new RevokeChecklistAccessCommandHandler(
            _readRepositoryMock.Object,
            _repositoryMock.Object,
            new Mock<ILogger<RevokeChecklistAccessCommandHandler>>().Object);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_RevokesAccessAndReturnsSuccess()
    {
        var checklistId = Guid.NewGuid();
        var checklist = BuildChecklist(checklistId, OwnerId);

        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId)).ReturnsAsync(checklist);

        var result = await _handler.HandleAsync(new RevokeChecklistAccessCommand(checklistId, OwnerId, TargetUserId));

        Assert.True(result.Succeeded);
        _repositoryMock.Verify(r => r.RevokeAccessAsync(checklistId, TargetUserId), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ChecklistNotFound_ReturnsFailure()
    {
        var checklistId = Guid.NewGuid();
        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId)).ReturnsAsync((Checklist?)null);

        var result = await _handler.HandleAsync(new RevokeChecklistAccessCommand(checklistId, OwnerId, TargetUserId));

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.ChecklistNotFound, result.ErrorMessage);
        _repositoryMock.Verify(r => r.RevokeAccessAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NotOwner_ReturnsFailure()
    {
        var checklistId = Guid.NewGuid();
        var checklist = BuildChecklist(checklistId, "other-owner");

        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId)).ReturnsAsync(checklist);

        var result = await _handler.HandleAsync(new RevokeChecklistAccessCommand(checklistId, OwnerId, TargetUserId));

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.NotChecklistOwner, result.ErrorMessage);
        _repositoryMock.Verify(r => r.RevokeAccessAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_RepositoryThrows_PropagatesException()
    {
        var checklistId = Guid.NewGuid();
        var checklist = BuildChecklist(checklistId, OwnerId);

        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId)).ReturnsAsync(checklist);
        _repositoryMock.Setup(r => r.RevokeAccessAsync(checklistId, TargetUserId))
            .ThrowsAsync(new InvalidOperationException("DB Error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.HandleAsync(new RevokeChecklistAccessCommand(checklistId, OwnerId, TargetUserId)));
    }

    private static Checklist BuildChecklist(Guid id, string userId) =>
        new() { Id = id, UserId = userId, Title = "Test", Description = string.Empty };
}
