using Application.Common;
using Application.Interfaces;
using Application.UseCases.GetChecklistAccessList;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class GetChecklistAccessListQueryHandlerTests
{
    private const string OwnerId = "owner-123";

    private readonly Mock<IChecklistReadOnlyRepository> _readRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly GetChecklistAccessListQueryHandler _handler;

    public GetChecklistAccessListQueryHandlerTests()
    {
        _readRepositoryMock = new Mock<IChecklistReadOnlyRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new GetChecklistAccessListQueryHandler(
            _readRepositoryMock.Object,
            _userRepositoryMock.Object,
            new Mock<ILogger<GetChecklistAccessListQueryHandler>>().Object);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ReturnsAccessListWithUsernames()
    {
        var checklistId = Guid.NewGuid();
        var checklist = BuildChecklist(checklistId, OwnerId);
        var userIds = new List<string> { "user-1", "user-2" };
        var usernameMap = new Dictionary<string, string>
        {
            { "user-1", "alice" },
            { "user-2", "bob" }
        };

        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId)).ReturnsAsync(checklist);
        _readRepositoryMock.Setup(r => r.GetAccessUserIdsAsync(checklistId, default)).ReturnsAsync(userIds);
        _userRepositoryMock.Setup(r => r.GetUsernamesByIdsAsync(userIds)).ReturnsAsync(usernameMap);

        var result = await _handler.HandleAsync(new GetChecklistAccessListQuery(checklistId, OwnerId));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(result.Value, u => u.UserId == "user-1" && u.UserName == "alice");
        Assert.Contains(result.Value, u => u.UserId == "user-2" && u.UserName == "bob");
    }

    [Fact]
    public async Task HandleAsync_EmptyAccessList_ReturnsEmptyList()
    {
        var checklistId = Guid.NewGuid();
        var checklist = BuildChecklist(checklistId, OwnerId);

        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId)).ReturnsAsync(checklist);
        _readRepositoryMock.Setup(r => r.GetAccessUserIdsAsync(checklistId, default)).ReturnsAsync(new List<string>());
        _userRepositoryMock.Setup(r => r.GetUsernamesByIdsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, string>());

        var result = await _handler.HandleAsync(new GetChecklistAccessListQuery(checklistId, OwnerId));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task HandleAsync_ChecklistNotFound_ReturnsFailure()
    {
        var checklistId = Guid.NewGuid();
        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId)).ReturnsAsync((Checklist?)null);

        var result = await _handler.HandleAsync(new GetChecklistAccessListQuery(checklistId, OwnerId));

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.ChecklistNotFound, result.ErrorMessage);
        _readRepositoryMock.Verify(r => r.GetAccessUserIdsAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NotOwner_ReturnsFailure()
    {
        var checklistId = Guid.NewGuid();
        var checklist = BuildChecklist(checklistId, "other-owner");

        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId)).ReturnsAsync(checklist);

        var result = await _handler.HandleAsync(new GetChecklistAccessListQuery(checklistId, OwnerId));

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.NotChecklistOwner, result.ErrorMessage);
        _readRepositoryMock.Verify(r => r.GetAccessUserIdsAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_RepositoryThrows_PropagatesException()
    {
        var checklistId = Guid.NewGuid();
        var checklist = BuildChecklist(checklistId, OwnerId);

        _readRepositoryMock.Setup(r => r.GetByIdAsync(checklistId)).ReturnsAsync(checklist);
        _readRepositoryMock.Setup(r => r.GetAccessUserIdsAsync(checklistId, default))
            .ThrowsAsync(new InvalidOperationException("DB Error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.HandleAsync(new GetChecklistAccessListQuery(checklistId, OwnerId)));
    }

    private static Checklist BuildChecklist(Guid id, string userId) =>
        new() { Id = id, UserId = userId, Title = "Test", Description = string.Empty };
}
