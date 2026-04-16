using Application.Interfaces;
using Application.UseCases.GetChecklistsByIds;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class GetChecklistsByIdsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithEmptyIds_ReturnsEmptyListAndDoesNotCallRepository()
    {
        // Arrange
        var repositoryMock = new Mock<IChecklistRepository>();
        var loggerMock = new Mock<ILogger<GetChecklistsByIdsQueryHandler>>();
        var sut = new GetChecklistsByIdsQueryHandler(repositoryMock.Object, loggerMock.Object);

        // Act
        var result = await sut.HandleAsync(new GetChecklistsByIdsQuery(new List<Guid>()));

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
        repositoryMock.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithValidIds_ReturnsOnlyPublishedChecklists()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var checklists = new List<Checklist>
        {
            new Checklist { Id = id1, Title = "Published", Status = ChecklistStatus.Published },
            new Checklist { Id = id2, Title = "Draft", Status = ChecklistStatus.Draft }
        };

        var repositoryMock = new Mock<IChecklistRepository>();
        repositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(checklists);

        var loggerMock = new Mock<ILogger<GetChecklistsByIdsQueryHandler>>();
        var sut = new GetChecklistsByIdsQueryHandler(repositoryMock.Object, loggerMock.Object);

        // Act
        var result = await sut.HandleAsync(new GetChecklistsByIdsQuery(new List<Guid> { id1, id2 }));

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        Assert.Equal(id1, result.Value[0].Id);
        Assert.Equal(ChecklistStatus.Published, result.Value[0].Status);
        repositoryMock.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryReturnsNothing_ReturnsEmptyList()
    {
        // Arrange
        var repositoryMock = new Mock<IChecklistRepository>();
        repositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Checklist>());

        var loggerMock = new Mock<ILogger<GetChecklistsByIdsQueryHandler>>();
        var sut = new GetChecklistsByIdsQueryHandler(repositoryMock.Object, loggerMock.Object);

        // Act
        var result = await sut.HandleAsync(new GetChecklistsByIdsQuery(new List<Guid> { Guid.NewGuid() }));

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Value!);
    }
}
