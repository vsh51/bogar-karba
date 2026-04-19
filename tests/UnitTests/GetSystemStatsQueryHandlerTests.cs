using Application.Interfaces;
using Application.UseCases.GetSystemStats;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class GetSystemStatsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsCountsFromRepositories()
    {
        var checklistRepoMock = new Mock<IChecklistRepository>();
        var userRepoMock = new Mock<IUserRepository>();
        var loggerMock = new Mock<ILogger<GetSystemStatsQueryHandler>>();

        checklistRepoMock
            .Setup(x => x.GetTotalCountAsync())
            .ReturnsAsync(42);

        checklistRepoMock
            .Setup(x => x.GetCountByStatusAsync(ChecklistStatus.Published))
            .ReturnsAsync(12);

        checklistRepoMock
            .Setup(x => x.GetCountByStatusAsync(ChecklistStatus.Draft))
            .ReturnsAsync(20);

        checklistRepoMock
            .Setup(x => x.GetCountByStatusAsync(ChecklistStatus.Archived))
            .ReturnsAsync(10);

        userRepoMock
            .Setup(x => x.GetTotalCountAsync())
            .ReturnsAsync(15);

        var handler = new GetSystemStatsQueryHandler(
            checklistRepoMock.Object,
            userRepoMock.Object,
            loggerMock.Object);

        var result = await handler.HandleAsync(new GetSystemStatsQuery());

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(42, result.Value.TotalChecklists);
        Assert.Equal(15, result.Value.TotalUsers);
        Assert.Equal(12, result.Value.PublishedChecklists);
        Assert.Equal(20, result.Value.DraftChecklists);
        Assert.Equal(10, result.Value.ArchivedChecklists);

        checklistRepoMock.Verify(x => x.GetTotalCountAsync(), Times.Once);
        checklistRepoMock.Verify(x => x.GetCountByStatusAsync(ChecklistStatus.Published), Times.Once);
        checklistRepoMock.Verify(x => x.GetCountByStatusAsync(ChecklistStatus.Draft), Times.Once);
        checklistRepoMock.Verify(x => x.GetCountByStatusAsync(ChecklistStatus.Archived), Times.Once);
        userRepoMock.Verify(x => x.GetTotalCountAsync(), Times.Once);
    }
}
