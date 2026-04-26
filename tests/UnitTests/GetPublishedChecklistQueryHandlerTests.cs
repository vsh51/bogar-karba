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

    [Fact]
    public async Task HandleAsync_WhenChecklistIsPrivateAndUserIsOwner_ReturnsSuccess()
    {
        var checklistId = Guid.NewGuid();
        var ownerId = "owner-123";
        var cancellationToken = new CancellationTokenSource().Token;
        var query = new GetPublishedChecklistQuery(checklistId, ownerId);
        var checklist = CreateChecklist(checklistId, ownerId, isPublic: false);

        var repositoryMock = new Mock<IChecklistReadOnlyRepository>();
        repositoryMock
            .Setup(r => r.GetByIdWithSectionsAsync(checklistId, cancellationToken))
            .ReturnsAsync(checklist);

        var loggerMock = new Mock<ILogger<GetPublishedChecklistQueryHandler>>();
        var sut = new GetPublishedChecklistQueryHandler(repositoryMock.Object, loggerMock.Object);

        var result = await sut.HandleAsync(query, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(checklistId, result.Value.Id);
    }

    [Fact]
    public async Task HandleAsync_WhenChecklistIsPrivateAndInactiveAndUserIsOwner_ReturnsSuccess()
    {
        var checklistId = Guid.NewGuid();
        var ownerId = "owner-123";
        var cancellationToken = new CancellationTokenSource().Token;
        var query = new GetPublishedChecklistQuery(checklistId, ownerId);
        var checklist = CreateChecklist(checklistId, ownerId, isPublic: false, status: ChecklistStatus.Draft);

        var repositoryMock = new Mock<IChecklistReadOnlyRepository>();
        repositoryMock
            .Setup(r => r.GetByIdWithSectionsAsync(checklistId, cancellationToken))
            .ReturnsAsync(checklist);

        var loggerMock = new Mock<ILogger<GetPublishedChecklistQueryHandler>>();
        var sut = new GetPublishedChecklistQueryHandler(repositoryMock.Object, loggerMock.Object);

        var result = await sut.HandleAsync(query, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task HandleAsync_WhenChecklistIsPrivateAndUserHasNoAccess_ReturnsChecklistIsPrivateError()
    {
        var checklistId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;
        var query = new GetPublishedChecklistQuery(checklistId, "other-user");
        var checklist = CreateChecklist(checklistId, "owner-123", isPublic: false);

        var repositoryMock = new Mock<IChecklistReadOnlyRepository>();
        repositoryMock
            .Setup(r => r.GetByIdWithSectionsAsync(checklistId, cancellationToken))
            .ReturnsAsync(checklist);
        repositoryMock
            .Setup(r => r.HasAccessAsync(checklistId, "other-user", cancellationToken))
            .ReturnsAsync(false);

        var loggerMock = new Mock<ILogger<GetPublishedChecklistQueryHandler>>();
        var sut = new GetPublishedChecklistQueryHandler(repositoryMock.Object, loggerMock.Object);

        var result = await sut.HandleAsync(query, cancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(Application.Common.ResultErrors.ChecklistIsPrivate, result.ErrorMessage);
        repositoryMock.Verify(r => r.HasAccessAsync(checklistId, "other-user", cancellationToken), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenChecklistIsPrivateAndUserHasExplicitAccess_ReturnsSuccess()
    {
        var checklistId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;
        var query = new GetPublishedChecklistQuery(checklistId, "granted-user");
        var checklist = CreateChecklist(checklistId, "owner-123", isPublic: false);

        var repositoryMock = new Mock<IChecklistReadOnlyRepository>();
        repositoryMock
            .Setup(r => r.GetByIdWithSectionsAsync(checklistId, cancellationToken))
            .ReturnsAsync(checklist);
        repositoryMock
            .Setup(r => r.HasAccessAsync(checklistId, "granted-user", cancellationToken))
            .ReturnsAsync(true);

        var loggerMock = new Mock<ILogger<GetPublishedChecklistQueryHandler>>();
        var sut = new GetPublishedChecklistQueryHandler(repositoryMock.Object, loggerMock.Object);

        var result = await sut.HandleAsync(query, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        repositoryMock.Verify(r => r.HasAccessAsync(checklistId, "granted-user", cancellationToken), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenChecklistIsPrivateAndUserIsAnonymous_ReturnsChecklistIsPrivateError()
    {
        var checklistId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;
        var query = new GetPublishedChecklistQuery(checklistId);
        var checklist = CreateChecklist(checklistId, "owner-123", isPublic: false);

        var repositoryMock = new Mock<IChecklistReadOnlyRepository>();
        repositoryMock
            .Setup(r => r.GetByIdWithSectionsAsync(checklistId, cancellationToken))
            .ReturnsAsync(checklist);

        var loggerMock = new Mock<ILogger<GetPublishedChecklistQueryHandler>>();
        var sut = new GetPublishedChecklistQueryHandler(repositoryMock.Object, loggerMock.Object);

        var result = await sut.HandleAsync(query, cancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(Application.Common.ResultErrors.ChecklistIsPrivate, result.ErrorMessage);
        repositoryMock.Verify(r => r.HasAccessAsync(It.IsAny<Guid>(), It.IsAny<string>(), cancellationToken), Times.Never);
    }

    private static Checklist CreateChecklist(
        Guid checklistId,
        string userId = "",
        bool isPublic = true,
        ChecklistStatus status = ChecklistStatus.Published)
    {
        return new Checklist
        {
            Id = checklistId,
            UserId = userId,
            Title = "Checklist title",
            Description = "Checklist description",
            Status = status,
            IsPublic = isPublic,
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
