using Application.Interfaces;
using Application.UseCases.GetPublishedChecklist;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class GetPublishedChecklistQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenChecklistExists_ReturnsMappedResultAndCallsRepositoryOnce()
    {
        var checklistId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;
        var query = new GetPublishedChecklistQuery(checklistId);
        var checklist = CreateChecklist(checklistId);

        var repositoryMock = new Mock<IChecklistReadOnlyRepository>();
        repositoryMock
            .Setup(r => r.GetByIdWithSectionsAsync(checklistId, cancellationToken))
            .ReturnsAsync(checklist);

        var loggerMock = new Mock<ILogger<GetPublishedChecklistQueryHandler>>();
        var sut = new GetPublishedChecklistQueryHandler(
            repositoryMock.Object, loggerMock.Object);

        var result = await sut.HandleAsync(query, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(checklistId, result.Value.Id);
        Assert.Equal("Checklist title", result.Value.Title);
        Assert.Equal("Checklist description", result.Value.Description);

        Assert.Equal(2, result.Value.Sections.Count);
        Assert.Equal("First section", result.Value.Sections[0].Name);
        Assert.Equal("Second section", result.Value.Sections[1].Name);

        Assert.Equal(2, result.Value.Sections[0].Items.Count);
        Assert.Equal("Task with position 1", result.Value.Sections[0].Items[0].Content);
        Assert.Equal("Task with position 2", result.Value.Sections[0].Items[1].Content);

        repositoryMock.Verify(
            r => r.GetByIdWithSectionsAsync(checklistId, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenChecklistDoesNotExist_ReturnsFailureAndCallsRepositoryOnce()
    {
        var checklistId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;
        var query = new GetPublishedChecklistQuery(checklistId);

        var repositoryMock = new Mock<IChecklistReadOnlyRepository>();
        repositoryMock
            .Setup(r => r.GetByIdWithSectionsAsync(checklistId, cancellationToken))
            .ReturnsAsync((Checklist?)null);

        var loggerMock = new Mock<ILogger<GetPublishedChecklistQueryHandler>>();
        var sut = new GetPublishedChecklistQueryHandler(
            repositoryMock.Object, loggerMock.Object);

        var result = await sut.HandleAsync(query, cancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(Application.Common.ResultErrors.ChecklistNotFound, result.ErrorMessage);
        repositoryMock.Verify(
            r => r.GetByIdWithSectionsAsync(checklistId, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrows_PropagatesExceptionAndCallsRepositoryOnce()
    {
        var checklistId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;
        var query = new GetPublishedChecklistQuery(checklistId);

        var repositoryException = new InvalidOperationException("Repository failure");

        var repositoryMock = new Mock<IChecklistReadOnlyRepository>();
        repositoryMock
            .Setup(r => r.GetByIdWithSectionsAsync(checklistId, cancellationToken))
            .ThrowsAsync(repositoryException);

        var loggerMock = new Mock<ILogger<GetPublishedChecklistQueryHandler>>();
        var sut = new GetPublishedChecklistQueryHandler(
            repositoryMock.Object, loggerMock.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.HandleAsync(query, cancellationToken));

        Assert.Equal("Repository failure", exception.Message);
        repositoryMock.Verify(
            r => r.GetByIdWithSectionsAsync(checklistId, cancellationToken),
            Times.Once);
    }

    private static Checklist CreateChecklist(Guid checklistId)
    {
        return new Checklist
        {
            Id = checklistId,
            Title = "Checklist title",
            Description = "Checklist description",
            Status = ChecklistStatus.Published,
            Sections =
            [
                new Section
                {
                    Id = Guid.NewGuid(),
                    Name = "Second section",
                    Position = 2,
                    Tasks =
                    [
                        new TaskItem
                        {
                            Id = Guid.NewGuid(),
                            Content = "Second section task",
                            Position = 1
                        }
                    ]
                },
                new Section
                {
                    Id = Guid.NewGuid(),
                    Name = "First section",
                    Position = 1,
                    Tasks =
                    [
                        new TaskItem
                        {
                            Id = Guid.NewGuid(),
                            Content = "Task with position 2",
                            Position = 2
                        },
                        new TaskItem
                        {
                            Id = Guid.NewGuid(),
                            Content = "Task with position 1",
                            Position = 1
                        }
                    ]
                }
            ]
        };
    }
}
