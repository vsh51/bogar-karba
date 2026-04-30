using Application.Interfaces;
using Application.UseCases.GetSharedChecklists;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class GetSharedChecklistsQueryHandlerTests
{
    private readonly Mock<IChecklistReadOnlyRepository> _repositoryMock;
    private readonly Mock<ILogger<GetSharedChecklistsQueryHandler>> _loggerMock;
    private readonly GetSharedChecklistsQueryHandler _handler;

    public GetSharedChecklistsQueryHandlerTests()
    {
        _repositoryMock = new Mock<IChecklistReadOnlyRepository>();
        _loggerMock = new Mock<ILogger<GetSharedChecklistsQueryHandler>>();
        _loggerMock.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
        _handler = new GetSharedChecklistsQueryHandler(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSharedChecklists_WhenTheyExist()
    {
        var userId = "test-user-id";
        var sharedChecklists = new List<SharedChecklist>
        {
            new(new Checklist { Id = Guid.NewGuid(), Title = "Shared 1", UserId = "other-user" }, "John Doe"),
            new(new Checklist { Id = Guid.NewGuid(), Title = "Shared 2", UserId = "other-user-2" }, "Jane Smith")
        };

        _repositoryMock.Setup(repo => repo.GetSharedWithUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sharedChecklists);

        var result = await _handler.HandleAsync(new GetSharedChecklistsQuery(userId));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("Shared 1", result.Value[0].Title);
        Assert.Equal("John Doe", result.Value[0].UserName);
        Assert.Equal("Shared 2", result.Value[1].Title);
        Assert.Equal("Jane Smith", result.Value[1].UserName);
        _repositoryMock.Verify(repo => repo.GetSharedWithUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyList_WhenNoSharedChecklistsFound()
    {
        var userId = "empty-user";
        _repositoryMock.Setup(repo => repo.GetSharedWithUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SharedChecklist>());

        var result = await _handler.HandleAsync(new GetSharedChecklistsQuery(userId));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
        _repositoryMock.Verify(repo => repo.GetSharedWithUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldLogInformation()
    {
        var userId = "logging-user";
        _repositoryMock.Setup(repo => repo.GetSharedWithUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SharedChecklist>());

        await _handler.HandleAsync(new GetSharedChecklistsQuery(userId));

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
