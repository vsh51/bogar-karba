using Application.Common;
using Application.Interfaces;
using Application.UseCases.GetChecklistForEdit;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class GetChecklistForEditQueryHandlerTests
{
    private const string OwnerId = "user-123";

    private readonly Mock<IChecklistReadOnlyRepository> _repositoryMock;
    private readonly GetChecklistForEditQueryHandler _handler;

    public GetChecklistForEditQueryHandlerTests()
    {
        _repositoryMock = new Mock<IChecklistReadOnlyRepository>();
        var loggerMock = new Mock<ILogger<GetChecklistForEditQueryHandler>>();
        _handler = new GetChecklistForEditQueryHandler(_repositoryMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenOwnerRequestsChecklist_ReturnsMappedResult()
    {
        var checklist = BuildChecklist();
        _repositoryMock
            .Setup(r => r.GetByIdWithSectionsAsync(checklist.Id, default))
            .ReturnsAsync(checklist);

        var result = await _handler.HandleAsync(new GetChecklistForEditQuery(checklist.Id, OwnerId));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(checklist.Id, result.Value.Id);
        Assert.Equal(checklist.Title, result.Value.Title);
        Assert.Equal(checklist.Description, result.Value.Description);
        Assert.Single(result.Value.Sections);
        Assert.Equal(2, result.Value.Sections[0].Tasks.Count);
        _repositoryMock.Verify(r => r.GetByIdWithSectionsAsync(checklist.Id, default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenChecklistNotFound_ReturnsChecklistNotFoundError()
    {
        _repositoryMock
            .Setup(r => r.GetByIdWithSectionsAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Checklist?)null);

        var result = await _handler.HandleAsync(new GetChecklistForEditQuery(Guid.NewGuid(), OwnerId));

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.ChecklistNotFound, result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsNotOwner_ReturnsNotChecklistOwnerError()
    {
        var checklist = BuildChecklist("other-user");
        _repositoryMock
            .Setup(r => r.GetByIdWithSectionsAsync(checklist.Id, default))
            .ReturnsAsync(checklist);

        var result = await _handler.HandleAsync(new GetChecklistForEditQuery(checklist.Id, OwnerId));

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.NotChecklistOwner, result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_TasksOrderedByPosition_ReturnsTasksInOrder()
    {
        var checklist = BuildChecklist();
        _repositoryMock
            .Setup(r => r.GetByIdWithSectionsAsync(checklist.Id, default))
            .ReturnsAsync(checklist);

        var result = await _handler.HandleAsync(new GetChecklistForEditQuery(checklist.Id, OwnerId));

        Assert.True(result.Succeeded);
        var tasks = result.Value!.Sections[0].Tasks;
        Assert.Equal("Task at position 0", tasks[0].Content);
        Assert.Equal("Task at position 1", tasks[1].Content);
    }

    [Fact]
    public async Task HandleAsync_LinkFieldPreserved_ReturnsLinkInTaskResult()
    {
        var checklist = BuildChecklist();
        checklist.Sections[0].Tasks[0].Link = "https://example.com";
        _repositoryMock
            .Setup(r => r.GetByIdWithSectionsAsync(checklist.Id, default))
            .ReturnsAsync(checklist);

        var result = await _handler.HandleAsync(new GetChecklistForEditQuery(checklist.Id, OwnerId));

        Assert.True(result.Succeeded);
        Assert.Equal("https://example.com", result.Value!.Sections[0].Tasks[0].Link);
        Assert.Null(result.Value.Sections[0].Tasks[1].Link);
    }

    [Fact]
    public async Task HandleAsync_RepositoryThrows_PropagatesException()
    {
        _repositoryMock
            .Setup(r => r.GetByIdWithSectionsAsync(It.IsAny<Guid>(), default))
            .ThrowsAsync(new InvalidOperationException("DB Error"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(new GetChecklistForEditQuery(Guid.NewGuid(), OwnerId)));
    }

    private static Checklist BuildChecklist(string userId = OwnerId)
    {
        var sectionId = Guid.NewGuid();
        return new Checklist
        {
            Id = Guid.NewGuid(),
            Title = "Test Checklist",
            Description = "Test Description",
            UserId = userId,
            Sections =
            [
                new Section
                {
                    Id = sectionId,
                    Name = "Section 1",
                    Position = 0,
                    Tasks =
                    [
                        new TaskItem { Id = Guid.NewGuid(), Content = "Task at position 0", Position = 0, SectionId = sectionId },
                        new TaskItem { Id = Guid.NewGuid(), Content = "Task at position 1", Position = 1, SectionId = sectionId },
                    ]
                }
            ]
        };
    }
}
