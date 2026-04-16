using Application.Interfaces;
using Application.UseCases.RemoveChecklistItem;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class RemoveChecklistItemCommandHandlerTests
{
    private const string OwnerId = "user-123";

    private readonly Mock<IChecklistRepository> _repositoryMock;
    private readonly Mock<ILogger<RemoveChecklistItemCommandHandler>> _loggerMock;
    private readonly RemoveChecklistItemCommandHandler _handler;

    public RemoveChecklistItemCommandHandlerTests()
    {
        _repositoryMock = new Mock<IChecklistRepository>();
        _loggerMock = new Mock<ILogger<RemoveChecklistItemCommandHandler>>();
        _handler = new RemoveChecklistItemCommandHandler(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_RemovesTaskAndResequences()
    {
        var checklist = BuildChecklist();
        var section = checklist.Sections[0];
        var taskToRemove = section.Tasks[0];
        var remainingTask = section.Tasks[1];

        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new RemoveChecklistItemCommand(
            checklist.Id, OwnerId, taskToRemove.Id);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(taskToRemove, section.Tasks);
        Assert.Equal(0, remainingTask.Position);
        _repositoryMock.Verify(r => r.UpdateAsync(), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_RemoveLastTask_LeavesEmptySection()
    {
        var sectionId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var checklist = new Checklist
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            UserId = OwnerId,
            Sections =
            [
                new Section
                {
                    Id = sectionId,
                    Name = "Section",
                    Position = 0,
                    Tasks =
                    [
                        new TaskItem { Id = taskId, Content = "Only task", Position = 0, SectionId = sectionId },
                    ]
                }
            ]
        };

        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new RemoveChecklistItemCommand(checklist.Id, OwnerId, taskId);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.Succeeded);
        Assert.Empty(checklist.Sections[0].Tasks);
    }

    [Fact]
    public async Task HandleAsync_ChecklistNotFound_ReturnsFailure()
    {
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Checklist?)null);

        var command = new RemoveChecklistItemCommand(
            Guid.NewGuid(), OwnerId, Guid.NewGuid());

        var result = await _handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal("Checklist not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_WrongOwner_ReturnsFailure()
    {
        var checklist = BuildChecklist("other-user");
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new RemoveChecklistItemCommand(
            checklist.Id, OwnerId, Guid.NewGuid());

        var result = await _handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal("You can only modify your own checklists.", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_TaskNotFound_ReturnsFailure()
    {
        var checklist = BuildChecklist();
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new RemoveChecklistItemCommand(
            checklist.Id, OwnerId, Guid.NewGuid());

        var result = await _handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal("Item not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_RemoveMiddleTask_ResequencesCorrectly()
    {
        var checklist = BuildChecklist();
        var section = checklist.Sections[0];
        var taskToRemove = section.Tasks[1];
        var task0 = section.Tasks[0];
        var task2 = section.Tasks[2];

        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new RemoveChecklistItemCommand(
            checklist.Id, OwnerId, taskToRemove.Id);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.Succeeded);
        Assert.Equal(2, section.Tasks.Count);
        Assert.Equal(0, task0.Position);
        Assert.Equal(1, task2.Position);
    }

    private static Checklist BuildChecklist(string userId = OwnerId)
    {
        var sectionId = Guid.NewGuid();
        return new Checklist
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            Description = string.Empty,
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
                        new TaskItem { Id = Guid.NewGuid(), Content = "Task 1", Position = 0, SectionId = sectionId },
                        new TaskItem { Id = Guid.NewGuid(), Content = "Task 2", Position = 1, SectionId = sectionId },
                        new TaskItem { Id = Guid.NewGuid(), Content = "Task 3", Position = 2, SectionId = sectionId },
                    ]
                }
            ]
        };
    }
}
