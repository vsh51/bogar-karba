using Application.Interfaces;
using Application.UseCases.GetUserChecklists;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests;

public class GetUserChecklistsQueryHandlerTests
{
    private readonly Mock<IChecklistReadOnlyRepository> _repositoryMock;
    private readonly Mock<ILogger<GetUserChecklistsQueryHandler>> _loggerMock;
    private readonly GetUserChecklistsQueryHandler _handler;

    public GetUserChecklistsQueryHandlerTests()
    {
        _repositoryMock = new Mock<IChecklistReadOnlyRepository>();
        _loggerMock = new Mock<ILogger<GetUserChecklistsQueryHandler>>();
        _loggerMock.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
        _handler = new GetUserChecklistsQueryHandler(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUserChecklists_WhenTheyExist()
    {
        var userId = "test-user-id";
        var checklists = new List<Checklist>
        {
            new() { Id = Guid.NewGuid(), Title = "Checklist 1", Description = "Desc 1", UserId = userId },
            new() { Id = Guid.NewGuid(), Title = "Checklist 2", Description = "Desc 2", UserId = userId }
        };

        _repositoryMock.Setup(repo => repo.GetByUserIdAsync(userId)).ReturnsAsync(checklists);

        var result = await _handler.HandleAsync(new GetUserChecklistsQuery(userId));

        Assert.Equal(2, result.Checklists.Count);
        Assert.Equal("Checklist 1", result.Checklists[0].Title);
        Assert.Equal("Checklist 2", result.Checklists[1].Title);
        _repositoryMock.Verify(repo => repo.GetByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyList_WhenNoChecklistsFound()
    {
        var userId = "empty-user";
        _repositoryMock.Setup(repo => repo.GetByUserIdAsync(userId)).ReturnsAsync(new List<Checklist>());

        var result = await _handler.HandleAsync(new GetUserChecklistsQuery(userId));

        Assert.Empty(result.Checklists);
        _repositoryMock.Verify(repo => repo.GetByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldLogInformation()
    {
        var userId = "logging-user";
        _repositoryMock.Setup(repo => repo.GetByUserIdAsync(userId)).ReturnsAsync(new List<Checklist>());

        await _handler.HandleAsync(new GetUserChecklistsQuery(userId));

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
