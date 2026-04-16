using Application.Interfaces;
using Application.UseCases.GroupTasksIntoSection;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class GroupTasksIntoSectionCommandHandlerTests
{
    private const string OwnerId = "user-123";

    private readonly Mock<IChecklistRepository> _repositoryMock;
    private readonly Mock<ILogger<GroupTasksIntoSectionCommandHandler>> _loggerMock;
    private readonly GroupTasksIntoSectionCommandHandler _handler;

    public GroupTasksIntoSectionCommandHandlerTests()
    {
        _repositoryMock = new Mock<IChecklistRepository>();
        _loggerMock = new Mock<ILogger<GroupTasksIntoSectionCommandHandler>>();
        _handler = new GroupTasksIntoSectionCommandHandler(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesNewSectionAndMoveTasks()
    {
        var checklist = BuildChecklist();
        var section = checklist.Sections[0];
        var task1 = section.Tasks[0];
        var task2 = section.Tasks[1];

        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new GroupTasksIntoSectionCommand(
            checklist.Id, OwnerId, "New Group", [task1.Id, task2.Id]);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.Succeeded);
        Assert.NotEqual(Guid.Empty, result.Value);
        Assert.Equal(2, checklist.Sections.Count);

        var newSection = checklist.Sections.First(s => s.Id == result.Value);
        Assert.Equal("New Group", newSection.Name);
        Assert.Equal(2, newSection.Tasks.Count);
        Assert.DoesNotContain(task1, section.Tasks);
        Assert.DoesNotContain(task2, section.Tasks);

        _repositoryMock.Verify(r => r.AddSectionAsync(It.IsAny<Section>()), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ResequencesSourceSectionPositions()
    {
        var checklist = BuildChecklist();
        var section = checklist.Sections[0];
        var taskToGroup = section.Tasks[0];

        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new GroupTasksIntoSectionCommand(
            checklist.Id, OwnerId, "Grouped", [taskToGroup.Id]);

        await _handler.HandleAsync(command);

        for (var i = 0; i < section.Tasks.Count; i++)
        {
            Assert.Equal(i, section.Tasks[i].Position);
        }
    }

    [Fact]
    public async Task HandleAsync_EmptySectionName_ReturnsFailure()
    {
        var command = new GroupTasksIntoSectionCommand(
            Guid.NewGuid(), OwnerId, string.Empty, [Guid.NewGuid()]);

        var result = await _handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal("Section name is required.", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_EmptyTaskIds_ReturnsFailure()
    {
        var command = new GroupTasksIntoSectionCommand(
            Guid.NewGuid(), OwnerId, "Name", []);

        var result = await _handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal("At least one item must be selected.", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_ChecklistNotFound_ReturnsFailure()
    {
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Checklist?)null);

        var command = new GroupTasksIntoSectionCommand(
            Guid.NewGuid(), OwnerId, "Name", [Guid.NewGuid()]);

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

        var command = new GroupTasksIntoSectionCommand(
            checklist.Id, OwnerId, "Name", [Guid.NewGuid()]);

        var result = await _handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal("You can only modify your own checklists.", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_TaskNotInChecklist_ReturnsFailure()
    {
        var checklist = BuildChecklist();
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new GroupTasksIntoSectionCommand(
            checklist.Id, OwnerId, "Name", [Guid.NewGuid()]);

        var result = await _handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal("Some items do not belong to this checklist.", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_TasksFromMultipleSections_AllMovedToNewSection()
    {
        var checklist = BuildChecklistWithTwoSections();
        var task1 = checklist.Sections[0].Tasks[0];
        var task2 = checklist.Sections[1].Tasks[0];

        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new GroupTasksIntoSectionCommand(
            checklist.Id, OwnerId, "Cross-section group", [task1.Id, task2.Id]);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.Succeeded);
        var newSection = checklist.Sections.First(s => s.Id == result.Value);
        Assert.Equal(2, newSection.Tasks.Count);
        Assert.Equal(task1.Id, newSection.Tasks[0].Id);
        Assert.Equal(task2.Id, newSection.Tasks[1].Id);
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
