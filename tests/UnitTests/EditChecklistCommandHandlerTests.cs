using Application.Common;
using Application.Interfaces;
using Application.Options;
using Application.UseCases.EditChecklist;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace UnitTests;

public class EditChecklistCommandHandlerTests
{
    private const string OwnerId = "user-123";

    private readonly Mock<IChecklistRepository> _repositoryMock;
    private readonly Mock<ILogger<EditChecklistCommandHandler>> _loggerMock;
    private readonly EditChecklistCommandHandler _handler;

    public EditChecklistCommandHandlerTests()
    {
        _repositoryMock = new Mock<IChecklistRepository>();
        _loggerMock = new Mock<ILogger<EditChecklistCommandHandler>>();
        var options = Options.Create(new ChecklistOptions());
        _handler = new EditChecklistCommandHandler(_repositoryMock.Object, options, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_UpdatesTitleDescriptionAndCallsUpdateAsync()
    {
        var checklist = BuildChecklist();
        var sec = checklist.Sections[0];

        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new EditChecklistCommand(
            checklist.Id,
            OwnerId,
            "New Title",
            "New Description",
            null,
            [new EditSectionRequest(sec.Id, sec.Name, sec.Tasks.Select(t => new EditTaskRequest(t.Id, t.Content)).ToList())]);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.Succeeded);
        Assert.Equal("New Title", checklist.Title);
        Assert.Equal("New Description", checklist.Description);
        _repositoryMock.Verify(r => r.UpdateAsync(), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_RenameSection_UpdatesSectionName()
    {
        var checklist = BuildChecklist();
        var sec = checklist.Sections[0];

        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new EditChecklistCommand(
            checklist.Id,
            OwnerId,
            checklist.Title,
            checklist.Description,
            null,
            [new EditSectionRequest(sec.Id, "Renamed Section", sec.Tasks.Select(t => new EditTaskRequest(t.Id, t.Content)).ToList())]);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.Succeeded);
        Assert.Equal("Renamed Section", checklist.Sections[0].Name);
    }

    [Fact]
    public async Task HandleAsync_RenameTask_UpdatesTaskContent()
    {
        var checklist = BuildChecklist();
        var sec = checklist.Sections[0];
        var task = sec.Tasks[0];

        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new EditChecklistCommand(
            checklist.Id,
            OwnerId,
            checklist.Title,
            checklist.Description,
            null,
            [new EditSectionRequest(sec.Id, sec.Name, [new EditTaskRequest(task.Id, "Renamed Task"), new EditTaskRequest(sec.Tasks[1].Id, sec.Tasks[1].Content)])]);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.Succeeded);
        Assert.Equal("Renamed Task", checklist.Sections[0].Tasks.First(t => t.Id == task.Id).Content);
    }

    [Fact]
    public async Task HandleAsync_DeleteSection_RemovesSectionFromChecklist()
    {
        var checklist = BuildChecklist();

        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new EditChecklistCommand(
            checklist.Id,
            OwnerId,
            checklist.Title,
            checklist.Description,
            null,
            []);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.Succeeded);
        Assert.Empty(checklist.Sections);
        _repositoryMock.Verify(r => r.UpdateAsync(), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DeleteTask_RemovesTaskFromSection()
    {
        var checklist = BuildChecklist();
        var sec = checklist.Sections[0];
        var taskToKeep = sec.Tasks[0];

        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new EditChecklistCommand(
            checklist.Id,
            OwnerId,
            checklist.Title,
            checklist.Description,
            null,
            [new EditSectionRequest(sec.Id, sec.Name, [new EditTaskRequest(taskToKeep.Id, taskToKeep.Content)])]);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.Succeeded);
        Assert.Single(checklist.Sections[0].Tasks);
        Assert.Equal(taskToKeep.Id, checklist.Sections[0].Tasks[0].Id);
    }

    [Fact]
    public async Task HandleAsync_ChecklistNotFound_ReturnsFailure()
    {
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Checklist?)null);

        var command = new EditChecklistCommand(Guid.NewGuid(), OwnerId, "Title", string.Empty, null, []);

        var result = await _handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.ChecklistNotFound, result.ErrorMessage);
        _repositoryMock.Verify(r => r.UpdateAsync(), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WrongOwner_ReturnsFailure()
    {
        var checklist = BuildChecklist("other-user");
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new EditChecklistCommand(checklist.Id, OwnerId, "Title", string.Empty, null, []);

        var result = await _handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.NotChecklistOwner, result.ErrorMessage);
        _repositoryMock.Verify(r => r.UpdateAsync(), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_EmptyTitle_ReturnsFailure()
    {
        var command = new EditChecklistCommand(Guid.NewGuid(), OwnerId, string.Empty, string.Empty, null, []);

        var result = await _handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.TitleRequired, result.ErrorMessage);
        _repositoryMock.Verify(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>()), Times.Never);
        _repositoryMock.Verify(r => r.UpdateAsync(), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NewSectionId_ReturnsFailure()
    {
        var checklist = BuildChecklist();
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new EditChecklistCommand(
            checklist.Id,
            OwnerId,
            checklist.Title,
            checklist.Description,
            null,
            [new EditSectionRequest(Guid.NewGuid(), "New Section", [])]);

        var result = await _handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.AddingSectionsNotAllowed, result.ErrorMessage);
        _repositoryMock.Verify(r => r.UpdateAsync(), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NewTaskId_ReturnsFailure()
    {
        var checklist = BuildChecklist();
        var sec = checklist.Sections[0];

        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new EditChecklistCommand(
            checklist.Id,
            OwnerId,
            checklist.Title,
            checklist.Description,
            null,
            [new EditSectionRequest(sec.Id, sec.Name, [new EditTaskRequest(Guid.NewGuid(), "New Task")])]);

        var result = await _handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.AddingTasksNotAllowed, result.ErrorMessage);
        _repositoryMock.Verify(r => r.UpdateAsync(), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithLink_SavesLinkOnTask()
    {
        var checklist = BuildChecklist();
        var sec = checklist.Sections[0];
        var task = sec.Tasks[0];

        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new EditChecklistCommand(
            checklist.Id,
            OwnerId,
            checklist.Title,
            checklist.Description,
            null,
            [new EditSectionRequest(sec.Id, sec.Name, [
                new EditTaskRequest(task.Id, task.Content, "https://example.com"),
                new EditTaskRequest(sec.Tasks[1].Id, sec.Tasks[1].Content)
            ])]);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.Succeeded);
        Assert.Equal("https://example.com", checklist.Sections[0].Tasks.First(t => t.Id == task.Id).Link);
    }

    [Fact]
    public async Task HandleAsync_WithWhitespaceLink_SavesNullOnTask()
    {
        var checklist = BuildChecklist();
        var sec = checklist.Sections[0];
        var task = sec.Tasks[0];
        task.Link = "https://old-link.com";

        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new EditChecklistCommand(
            checklist.Id,
            OwnerId,
            checklist.Title,
            checklist.Description,
            null,
            [new EditSectionRequest(sec.Id, sec.Name, [
                new EditTaskRequest(task.Id, task.Content, "   "),
                new EditTaskRequest(sec.Tasks[1].Id, sec.Tasks[1].Content)
            ])]);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.Succeeded);
        Assert.Null(checklist.Sections[0].Tasks.First(t => t.Id == task.Id).Link);
    }

    [Fact]
    public async Task HandleAsync_RepositoryThrows_PropagatesException()
    {
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("DB Error"));

        var command = new EditChecklistCommand(Guid.NewGuid(), OwnerId, "Title", string.Empty, null, []);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(command));
    }

    private static Checklist BuildChecklist(string userId = OwnerId)
    {
        return new Checklist
        {
            Id = Guid.NewGuid(),
            Title = "Original Title",
            Description = "Original Description",
            UserId = userId,
            Sections =
            [
                new Section
                {
                    Id = Guid.NewGuid(),
                    Name = "Section 1",
                    Position = 0,
                    Tasks =
                    [
                        new TaskItem { Id = Guid.NewGuid(), Content = "Task 1", Position = 0 },
                        new TaskItem { Id = Guid.NewGuid(), Content = "Task 2", Position = 1 }
                    ]
                }
            ]
        };
    }
}
