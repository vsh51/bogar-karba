using Application.Interfaces;
using Application.UseCases.ExportChecklist;
using Application.UseCases.ExportChecklist.Markdown;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class ExportMarkdownQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenChecklistExists_ReturnsMarkdownWithProgress()
    {
        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();
        var taskId3 = Guid.NewGuid();
        var checklistId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;
        var checklist = CreateChecklist(checklistId, taskId1, taskId2, taskId3);
        var query = new ExportChecklistQuery(checklistId, [taskId1, taskId3]);

        var repositoryMock = new Mock<IChecklistReadOnlyRepository>();
        repositoryMock
            .Setup(r => r.GetPublishedChecklistAsync(checklistId, cancellationToken))
            .ReturnsAsync(checklist);

        var loggerMock = new Mock<ILogger<ExportMarkdownQueryHandler>>();
        var sut = new ExportMarkdownQueryHandler(
            repositoryMock.Object, loggerMock.Object);

        var result = await sut.HandleAsync(query, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);

        var expected = string.Join(
            Environment.NewLine,
            "# Test checklist",
            string.Empty,
            "Test description",
            string.Empty,
            "## First section",
            string.Empty,
            "- [+] First task",
            "- [ ] Second task",
            string.Empty,
            "## Second section",
            string.Empty,
            "- [+] Third task");

        Assert.Equal(expected, result.Value.Content);
        repositoryMock.Verify(
            r => r.GetPublishedChecklistAsync(checklistId, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenNoTasksCompleted_ReturnsAllUnchecked()
    {
        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();
        var taskId3 = Guid.NewGuid();
        var checklistId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;
        var checklist = CreateChecklist(checklistId, taskId1, taskId2, taskId3);
        var query = new ExportChecklistQuery(checklistId, []);

        var repositoryMock = new Mock<IChecklistReadOnlyRepository>();
        repositoryMock
            .Setup(r => r.GetPublishedChecklistAsync(checklistId, cancellationToken))
            .ReturnsAsync(checklist);

        var loggerMock = new Mock<ILogger<ExportMarkdownQueryHandler>>();
        var sut = new ExportMarkdownQueryHandler(
            repositoryMock.Object, loggerMock.Object);

        var result = await sut.HandleAsync(query, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.DoesNotContain("[+]", result.Value!.Content);
        Assert.Contains("[ ]", result.Value.Content);
    }

    [Fact]
    public async Task HandleAsync_WhenChecklistDoesNotExist_ReturnsFailure()
    {
        var checklistId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;
        var query = new ExportChecklistQuery(checklistId, []);

        var repositoryMock = new Mock<IChecklistReadOnlyRepository>();
        repositoryMock
            .Setup(r => r.GetPublishedChecklistAsync(checklistId, cancellationToken))
            .ReturnsAsync((Checklist?)null);

        var loggerMock = new Mock<ILogger<ExportMarkdownQueryHandler>>();
        var sut = new ExportMarkdownQueryHandler(
            repositoryMock.Object, loggerMock.Object);

        var result = await sut.HandleAsync(query, cancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal("Checklist not found or not published.", result.ErrorMessage);
        repositoryMock.Verify(
            r => r.GetPublishedChecklistAsync(checklistId, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrows_PropagatesException()
    {
        var checklistId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;
        var query = new ExportChecklistQuery(checklistId, []);
        var repositoryException = new InvalidOperationException("Repository failure");

        var repositoryMock = new Mock<IChecklistReadOnlyRepository>();
        repositoryMock
            .Setup(r => r.GetPublishedChecklistAsync(checklistId, cancellationToken))
            .ThrowsAsync(repositoryException);

        var loggerMock = new Mock<ILogger<ExportMarkdownQueryHandler>>();
        var sut = new ExportMarkdownQueryHandler(
            repositoryMock.Object, loggerMock.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.HandleAsync(query, cancellationToken));

        Assert.Equal("Repository failure", exception.Message);
        repositoryMock.Verify(
            r => r.GetPublishedChecklistAsync(checklistId, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenChecklistHasNoDescription_OmitsDescriptionLine()
    {
        var checklistId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;

        var checklist = new Checklist
        {
            Id = checklistId,
            Title = "No description",
            Description = string.Empty,
            Status = ChecklistStatus.Published,
            Sections =
            [
                new Section
                {
                    Id = Guid.NewGuid(),
                    Name = "Section",
                    Position = 1,
                    Tasks =
                    [
                        new TaskItem { Id = taskId, Content = "Task", Position = 1 }
                    ]
                }
            ]
        };

        var query = new ExportChecklistQuery(checklistId, []);

        var repositoryMock = new Mock<IChecklistReadOnlyRepository>();
        repositoryMock
            .Setup(r => r.GetPublishedChecklistAsync(checklistId, cancellationToken))
            .ReturnsAsync(checklist);

        var loggerMock = new Mock<ILogger<ExportMarkdownQueryHandler>>();
        var sut = new ExportMarkdownQueryHandler(
            repositoryMock.Object, loggerMock.Object);

        var result = await sut.HandleAsync(query, cancellationToken);

        Assert.True(result.Succeeded);

        var expected = string.Join(
            Environment.NewLine,
            "# No description",
            string.Empty,
            "## Section",
            string.Empty,
            "- [ ] Task");

        Assert.Equal(expected, result.Value!.Content);
    }

    private static Checklist CreateChecklist(
        Guid checklistId, Guid taskId1, Guid taskId2, Guid taskId3)
    {
        return new Checklist
        {
            Id = checklistId,
            Title = "Test checklist",
            Description = "Test description",
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
                        new TaskItem { Id = taskId3, Content = "Third task", Position = 1 }
                    ]
                },
                new Section
                {
                    Id = Guid.NewGuid(),
                    Name = "First section",
                    Position = 1,
                    Tasks =
                    [
                        new TaskItem { Id = taskId2, Content = "Second task", Position = 2 },
                        new TaskItem { Id = taskId1, Content = "First task", Position = 1 }
                    ]
                }
            ]
        };
    }
}
