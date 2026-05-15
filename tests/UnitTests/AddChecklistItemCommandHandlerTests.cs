using Application.Common;
using Application.Interfaces;
using Application.UseCases.AddChecklistItem;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class AddChecklistItemCommandHandlerTests
{
    private const string OwnerId = "user-123";

    private readonly Mock<IChecklistRepository> _repositoryMock;
    private readonly Mock<ILogger<AddChecklistItemCommandHandler>> _loggerMock;
    private readonly AddChecklistItemCommandHandler _handler;

    public AddChecklistItemCommandHandlerTests()
    {
        _repositoryMock = new Mock<IChecklistRepository>();
        _loggerMock = new Mock<ILogger<AddChecklistItemCommandHandler>>();
        _handler = new AddChecklistItemCommandHandler(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_AddsTaskAndReturnsId()
    {
        var checklist = BuildChecklist();
        var section = checklist.Sections[0];
        var initialCount = section.Tasks.Count;

        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new AddChecklistItemCommand(
            checklist.Id, OwnerId, section.Id, "New task");

        var result = await _handler.HandleAsync(command);

        Assert.True(result.Succeeded);
        Assert.NotEqual(Guid.Empty, result.Value);
        Assert.Equal(initialCount + 1, section.Tasks.Count);
        Assert.Equal("New task", section.Tasks[^1].Content);
        Assert.Equal(initialCount, section.Tasks[^1].Position);

        _repositoryMock.Verify(r => r.AddTaskAsync(It.IsAny<TaskItem>()), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_EmptyContent_ReturnsFailure()
    {
        var command = new AddChecklistItemCommand(
            Guid.NewGuid(), OwnerId, Guid.NewGuid(), "   ");

        var result = await _handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.ItemContentRequired, result.ErrorMessage);
        _repositoryMock.Verify(r => r.UpdateAsync(), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ChecklistNotFound_ReturnsFailure()
    {
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Checklist?)null);

        var command = new AddChecklistItemCommand(
            Guid.NewGuid(), OwnerId, Guid.NewGuid(), "Content");

        var result = await _handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.ChecklistNotFound, result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_WrongOwner_ReturnsFailure()
    {
        var checklist = BuildChecklist("other-user");
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new AddChecklistItemCommand(
            checklist.Id, OwnerId, checklist.Sections[0].Id, "Content");

        var result = await _handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.NotChecklistOwner, result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_SectionNotFound_ReturnsFailure()
    {
        var checklist = BuildChecklist();
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new AddChecklistItemCommand(
            checklist.Id, OwnerId, Guid.NewGuid(), "Content");

        var result = await _handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.SectionNotFound, result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_WithLink_SavesLinkOnTask()
    {
        var checklist = BuildChecklist();
        var section = checklist.Sections[0];

        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new AddChecklistItemCommand(
            checklist.Id, OwnerId, section.Id, "New task", "https://example.com");

        var result = await _handler.HandleAsync(command);

        Assert.True(result.Succeeded);
        Assert.Equal("https://example.com", section.Tasks[^1].Link);
    }

    [Fact]
    public async Task HandleAsync_WithWhitespaceLink_SavesNullOnTask()
    {
        var checklist = BuildChecklist();
        var section = checklist.Sections[0];

        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(checklist.Id))
            .ReturnsAsync(checklist);

        var command = new AddChecklistItemCommand(
            checklist.Id, OwnerId, section.Id, "New task", "   ");

        var result = await _handler.HandleAsync(command);

        Assert.True(result.Succeeded);
        Assert.Null(section.Tasks[^1].Link);
    }

    [Fact]
    public async Task HandleAsync_RepositoryThrows_PropagatesException()
    {
        _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("DB Error"));

        var command = new AddChecklistItemCommand(
            Guid.NewGuid(), OwnerId, Guid.NewGuid(), "Content");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(command));
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
                    ]
                }
            ]
        };
    }
}
