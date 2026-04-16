using Application.Common;
using Application.Interfaces;
using Application.UseCases.CloneChecklist;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class CloneChecklistCommandHandlerTests
{
    private readonly Mock<IChecklistRepository> _repositoryMock;
    private readonly Mock<IChecklistReadOnlyRepository> _readRepositoryMock;
    private readonly Mock<ILogger<CloneChecklistCommandHandler>> _loggerMock;
    private readonly CloneChecklistCommandHandler _handler;

    public CloneChecklistCommandHandlerTests()
    {
        _repositoryMock = new Mock<IChecklistRepository>();
        _readRepositoryMock = new Mock<IChecklistReadOnlyRepository>();
        _loggerMock = new Mock<ILogger<CloneChecklistCommandHandler>>();

        _handler = new CloneChecklistCommandHandler(
            _repositoryMock.Object,
            _readRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnFailureWhenSourceChecklistNotFound()
    {
        var checklistId = Guid.NewGuid();
        _readRepositoryMock
            .Setup(r => r.GetByIdWithSectionsAsync(checklistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Checklist?)null);

        var result = await _handler.HandleAsync(new CloneChecklistCommand(checklistId, "user-1"));

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.ChecklistNotFound, result.ErrorMessage);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Checklist>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnFailureWhenUserDoesNotOwnChecklist()
    {
        var checklistId = Guid.NewGuid();
        var sourceChecklist = new Checklist
        {
            Id = checklistId,
            UserId = "owner-1",
            Title = "Original",
            Description = "Description"
        };

        _readRepositoryMock
            .Setup(r => r.GetByIdWithSectionsAsync(checklistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceChecklist);

        var result = await _handler.HandleAsync(new CloneChecklistCommand(checklistId, "user-2"));

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.NotChecklistOwner, result.ErrorMessage);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Checklist>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsyncShouldCloneChecklistWithCopySuffixAndNestedItems()
    {
        var checklistId = Guid.NewGuid();
        var sourceChecklist = new Checklist
        {
            Id = checklistId,
            UserId = "owner-1",
            Title = "Original",
            Description = "Description",
            Status = ChecklistStatus.Published,
            Sections =
            [
                new Section
                {
                    Id = Guid.NewGuid(),
                    Name = "Section 1",
                    Position = 2,
                    Tasks =
                    [
                        new TaskItem { Id = Guid.NewGuid(), Content = "Task B", Position = 2 },
                        new TaskItem { Id = Guid.NewGuid(), Content = "Task A", Position = 1 },
                    ]
                },
                new Section
                {
                    Id = Guid.NewGuid(),
                    Name = "Section 0",
                    Position = 1,
                    Tasks =
                    [
                        new TaskItem { Id = Guid.NewGuid(), Content = "Task Z", Position = 1 },
                    ]
                }
            ]
        };

        _readRepositoryMock
            .Setup(r => r.GetByIdWithSectionsAsync(checklistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceChecklist);

        Checklist? persistedChecklist = null;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Checklist>()))
            .Callback<Checklist>(checklist => persistedChecklist = checklist)
            .Returns(Task.CompletedTask);

        var result = await _handler.HandleAsync(new CloneChecklistCommand(checklistId, "owner-1"));

        Assert.True(result.Succeeded);
        Assert.NotEqual(Guid.Empty, result.Value);
        Assert.NotNull(persistedChecklist);

        Assert.Equal("Original (Copy)", persistedChecklist!.Title);
        Assert.Equal("Description", persistedChecklist.Description);
        Assert.Equal(ChecklistStatus.Draft, persistedChecklist.Status);
        Assert.Equal("owner-1", persistedChecklist.UserId);
        Assert.Equal(2, persistedChecklist.Sections.Count);

        Assert.Equal("Section 0", persistedChecklist.Sections[0].Name);
        Assert.Equal("Section 1", persistedChecklist.Sections[1].Name);
        Assert.Equal("Task A", persistedChecklist.Sections[1].Tasks[0].Content);
        Assert.Equal("Task B", persistedChecklist.Sections[1].Tasks[1].Content);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Checklist>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnFailureWhenRepositoryThrows()
    {
        var checklistId = Guid.NewGuid();
        var sourceChecklist = new Checklist
        {
            Id = checklistId,
            UserId = "owner-1",
            Title = "Original"
        };

        _readRepositoryMock
            .Setup(r => r.GetByIdWithSectionsAsync(checklistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceChecklist);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Checklist>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var result = await _handler.HandleAsync(new CloneChecklistCommand(checklistId, "owner-1"));

        Assert.False(result.Succeeded);
        Assert.Equal("An error occurred while cloning the checklist.", result.ErrorMessage);
    }
}
