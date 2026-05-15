using Application.Common;
using Application.Interfaces;
using Application.UseCases.GrantChecklistAccess;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class GrantChecklistAccessCommandHandlerTests
{
    private const string OwnerId = "owner-123";
    private const string TargetUsername = "targetuser";
    private const string TargetUserId = "target-456";

    private readonly Mock<IChecklistReadOnlyRepository> _readRepositoryMock;
    private readonly Mock<IChecklistRepository> _repositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly GrantChecklistAccessCommandHandler _handler;

    public GrantChecklistAccessCommandHandlerTests()
    {
        _readRepositoryMock = new Mock<IChecklistReadOnlyRepository>();
        _repositoryMock = new Mock<IChecklistRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new GrantChecklistAccessCommandHandler(
            _readRepositoryMock.Object,
            _repositoryMock.Object,
            _userRepositoryMock.Object,
            new Mock<ILogger<GrantChecklistAccessCommandHandler>>().Object);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_GrantsAccessAndReturnsSuccess()
    {
        var checklistId = Guid.NewGuid();
        var checklist = BuildChecklist(checklistId, OwnerId);

        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId)).ReturnsAsync(checklist);
        _userRepositoryMock.Setup(r => r.GetUserIdByUsernameAsync(TargetUsername)).ReturnsAsync(TargetUserId);
        _readRepositoryMock.Setup(r => r.HasAccessAsync(checklistId, TargetUserId, default)).ReturnsAsync(false);

        var result = await _handler.HandleAsync(new GrantChecklistAccessCommand(checklistId, OwnerId, TargetUsername));

        Assert.True(result.Succeeded);
        _repositoryMock.Verify(
            r => r.GrantAccessAsync(It.Is<ChecklistAccess>(a =>
                a.ChecklistId == checklistId && a.UserId == TargetUserId)),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ChecklistNotFound_ReturnsFailure()
    {
        var checklistId = Guid.NewGuid();
        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId)).ReturnsAsync((Checklist?)null);

        var result = await _handler.HandleAsync(new GrantChecklistAccessCommand(checklistId, OwnerId, TargetUsername));

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.ChecklistNotFound, result.ErrorMessage);
        _repositoryMock.Verify(r => r.GrantAccessAsync(It.IsAny<ChecklistAccess>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NotOwner_ReturnsFailure()
    {
        var checklistId = Guid.NewGuid();
        var checklist = BuildChecklist(checklistId, "other-owner");

        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId)).ReturnsAsync(checklist);

        var result = await _handler.HandleAsync(new GrantChecklistAccessCommand(checklistId, OwnerId, TargetUsername));

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.NotChecklistOwner, result.ErrorMessage);
        _repositoryMock.Verify(r => r.GrantAccessAsync(It.IsAny<ChecklistAccess>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_UsernameNotFound_ReturnsFailure()
    {
        var checklistId = Guid.NewGuid();
        var checklist = BuildChecklist(checklistId, OwnerId);

        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId)).ReturnsAsync(checklist);
        _userRepositoryMock.Setup(r => r.GetUserIdByUsernameAsync(TargetUsername)).ReturnsAsync((string?)null);

        var result = await _handler.HandleAsync(new GrantChecklistAccessCommand(checklistId, OwnerId, TargetUsername));

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.UserNotFound, result.ErrorMessage);
        _repositoryMock.Verify(r => r.GrantAccessAsync(It.IsAny<ChecklistAccess>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_GrantingAccessToOwner_ReturnsFailure()
    {
        var checklistId = Guid.NewGuid();
        var checklist = BuildChecklist(checklistId, OwnerId);

        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId)).ReturnsAsync(checklist);
        _userRepositoryMock.Setup(r => r.GetUserIdByUsernameAsync(TargetUsername)).ReturnsAsync(OwnerId);

        var result = await _handler.HandleAsync(new GrantChecklistAccessCommand(checklistId, OwnerId, TargetUsername));

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.CannotGrantAccessToOwner, result.ErrorMessage);
        _repositoryMock.Verify(r => r.GrantAccessAsync(It.IsAny<ChecklistAccess>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_UserAlreadyHasAccess_ReturnsFailure()
    {
        var checklistId = Guid.NewGuid();
        var checklist = BuildChecklist(checklistId, OwnerId);

        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId)).ReturnsAsync(checklist);
        _userRepositoryMock.Setup(r => r.GetUserIdByUsernameAsync(TargetUsername)).ReturnsAsync(TargetUserId);
        _readRepositoryMock.Setup(r => r.HasAccessAsync(checklistId, TargetUserId, default)).ReturnsAsync(true);

        var result = await _handler.HandleAsync(new GrantChecklistAccessCommand(checklistId, OwnerId, TargetUsername));

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.UserAlreadyHasAccess, result.ErrorMessage);
        _repositoryMock.Verify(r => r.GrantAccessAsync(It.IsAny<ChecklistAccess>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_RepositoryThrows_PropagatesException()
    {
        var checklistId = Guid.NewGuid();
        var checklist = BuildChecklist(checklistId, OwnerId);

        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId)).ReturnsAsync(checklist);
        _userRepositoryMock.Setup(r => r.GetUserIdByUsernameAsync(TargetUsername)).ReturnsAsync(TargetUserId);
        _readRepositoryMock.Setup(r => r.HasAccessAsync(checklistId, TargetUserId, default)).ReturnsAsync(false);
        _repositoryMock.Setup(r => r.GrantAccessAsync(It.IsAny<ChecklistAccess>()))
            .ThrowsAsync(new InvalidOperationException("DB Error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.HandleAsync(new GrantChecklistAccessCommand(checklistId, OwnerId, TargetUsername)));
    }

    private static Checklist BuildChecklist(Guid id, string userId) =>
        new() { Id = id, UserId = userId, Title = "Test", Description = string.Empty };
}
