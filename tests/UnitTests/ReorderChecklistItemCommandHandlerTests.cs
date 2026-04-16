using Application.Interfaces;
using Application.UseCases.ReorderChecklistItem;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class ReorderChecklistItemCommandHandlerTests
{
    private const string OwnerId = "user-123";

    private readonly Mock<IChecklistRepository> _repositoryMock;
    private readonly Mock<ILogger<ReorderChecklistItemCommandHandler>> _loggerMock;
    private readonly ReorderChecklistItemCommandHandler _handler;

    public ReorderChecklistItemCommandHandlerTests()
    {
        _repositoryMock = new Mock<IChecklistRepository>();
        _loggerMock = new Mock<ILogger<ReorderChecklistItemCommandHandler>>();
        _handler = new ReorderChecklistItemCommandHandler(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_MoveTaskWithinSection_ResequencesPositions()
    {
        var checklist = BuildChecklist();
        var section = checklist.Sections[0];
        var taskToMove = section.Tasks[0];

        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new ReorderChecklistItemCommand(
            checklist.Id, OwnerId, taskToMove.Id, section.Id, 2);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.Succeeded);
        Assert.Equal(2, section.Tasks.IndexOf(taskToMove));
        for (var i = 0; i < section.Tasks.Count; i++)
        {
            Assert.Equal(i, section.Tasks[i].Position);
        }

        _repositoryMock.Verify(r => r.UpdateAsync(), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_MoveTaskBetweenSections_UpdatesSectionIdAndPositions()
    {
        var checklist = BuildChecklistWithTwoSections();
        var source = checklist.Sections[0];
        var target = checklist.Sections[1];
        var taskToMove = source.Tasks[0];

        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new ReorderChecklistItemCommand(
            checklist.Id, OwnerId, taskToMove.Id, target.Id, 0);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.Succeeded);
        Assert.Equal(target.Id, taskToMove.SectionId);
        Assert.Contains(taskToMove, target.Tasks);
        Assert.DoesNotContain(taskToMove, source.Tasks);
        _repositoryMock.Verify(r => r.UpdateAsync(), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NegativePosition_ReturnsFailure()
    {
        var command = new ReorderChecklistItemCommand(
            Guid.NewGuid(), OwnerId, Guid.NewGuid(), Guid.NewGuid(), -1);

        var result = await _handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal("Position must be non-negative.", result.ErrorMessage);
        _repositoryMock.Verify(r => r.UpdateAsync(), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ChecklistNotFound_ReturnsFailure()
    {
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Checklist?)null);

        var command = new ReorderChecklistItemCommand(
            Guid.NewGuid(), OwnerId, Guid.NewGuid(), Guid.NewGuid(), 0);

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

        var command = new ReorderChecklistItemCommand(
            checklist.Id, OwnerId, Guid.NewGuid(), Guid.NewGuid(), 0);

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

        var command = new ReorderChecklistItemCommand(
            checklist.Id, OwnerId, Guid.NewGuid(), checklist.Sections[0].Id, 0);

        var result = await _handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal("Item not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_TargetSectionNotFound_ReturnsFailure()
    {
        var checklist = BuildChecklist();
        var taskId = checklist.Sections[0].Tasks[0].Id;
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new ReorderChecklistItemCommand(
            checklist.Id, OwnerId, taskId, Guid.NewGuid(), 0);

        var result = await _handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal("Target section not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_PositionClamped_DoesNotExceedTaskCount()
    {
        var checklist = BuildChecklist();
        var section = checklist.Sections[0];
        var taskToMove = section.Tasks[0];

        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new ReorderChecklistItemCommand(
            checklist.Id, OwnerId, taskToMove.Id, section.Id, 999);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.Succeeded);
        Assert.Equal(section.Tasks.Count - 1, section.Tasks.IndexOf(taskToMove));
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

    private static Checklist BuildChecklistWithTwoSections()
    {
        var sectionId1 = Guid.NewGuid();
        var sectionId2 = Guid.NewGuid();
        return new Checklist
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            Description = string.Empty,
            UserId = OwnerId,
            Sections =
            [
                new Section
                {
                    Id = sectionId1,
                    Name = "Section 1",
                    Position = 0,
                    Tasks =
                    [
                        new TaskItem { Id = Guid.NewGuid(), Content = "A1", Position = 0, SectionId = sectionId1 },
                        new TaskItem { Id = Guid.NewGuid(), Content = "A2", Position = 1, SectionId = sectionId1 },
                    ]
                },
                new Section
                {
                    Id = sectionId2,
                    Name = "Section 2",
                    Position = 1,
                    Tasks =
                    [
                        new TaskItem { Id = Guid.NewGuid(), Content = "B1", Position = 0, SectionId = sectionId2 },
                    ]
                }
            ]
        };
    }
}
