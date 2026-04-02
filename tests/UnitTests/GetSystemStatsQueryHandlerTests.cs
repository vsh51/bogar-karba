using Application.Interfaces;
using Application.UseCases.GetSystemStats;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

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

        checklistRepoMock.Verify(x => x.GetTotalCountAsync(), Times.Once);
        userRepoMock.Verify(x => x.GetTotalCountAsync(), Times.Once);
    }
}
