using Application.Common;
using Application.Interfaces;
using Application.UseCases.SaveChecklistProgress;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class SaveChecklistProgressCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenChecklistNotFound_ReturnsChecklistNotFound()
    {
        var checklistId = Guid.NewGuid();
        var userId = "user-1";
        var cancellationToken = new CancellationTokenSource().Token;

        var readOnlyRepo = new Mock<IChecklistReadOnlyRepository>();
        readOnlyRepo
            .Setup(x => x.GetByIdWithSectionsAsync(checklistId, cancellationToken))
            .ReturnsAsync((Checklist?)null);

        var progressRepo = new Mock<IChecklistProgressRepository>();
        var logger = new Mock<ILogger<SaveChecklistProgressCommandHandler>>();

        var sut = new SaveChecklistProgressCommandHandler(readOnlyRepo.Object, progressRepo.Object, logger.Object);

        var result = await sut.HandleAsync(
            new SaveChecklistProgressCommand(checklistId, userId, [Guid.NewGuid()]),
            cancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.ChecklistNotFound, result.ErrorMessage);
        progressRepo.Verify(
            x => x.SaveCompletedTaskIdsAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<Guid>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsNotOwner_ReturnsNotChecklistOwner()
    {
        var checklistId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;
        var checklist = CreateChecklist(checklistId, ownerId: "owner-1");

        var readOnlyRepo = new Mock<IChecklistReadOnlyRepository>();
        readOnlyRepo
            .Setup(x => x.GetByIdWithSectionsAsync(checklistId, cancellationToken))
            .ReturnsAsync(checklist);

        var progressRepo = new Mock<IChecklistProgressRepository>();
        var logger = new Mock<ILogger<SaveChecklistProgressCommandHandler>>();

        var sut = new SaveChecklistProgressCommandHandler(readOnlyRepo.Object, progressRepo.Object, logger.Object);

        var result = await sut.HandleAsync(
            new SaveChecklistProgressCommand(checklistId, "another-user", [Guid.NewGuid()]),
            cancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(ResultErrors.NotChecklistOwner, result.ErrorMessage);
        progressRepo.Verify(
            x => x.SaveCompletedTaskIdsAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<Guid>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenOwnerSavesProgress_SavesOnlyValidDistinctTaskIds()
    {
        var checklistId = Guid.NewGuid();
        var ownerId = "owner-1";
        var cancellationToken = new CancellationTokenSource().Token;

        var validTaskId1 = Guid.NewGuid();
        var validTaskId2 = Guid.NewGuid();
        var invalidTaskId = Guid.NewGuid();

        var checklist = CreateChecklist(checklistId, ownerId, validTaskId1, validTaskId2);

        var readOnlyRepo = new Mock<IChecklistReadOnlyRepository>();
        readOnlyRepo
            .Setup(x => x.GetByIdWithSectionsAsync(checklistId, cancellationToken))
            .ReturnsAsync(checklist);

        IReadOnlyList<Guid>? capturedIds = null;

        var progressRepo = new Mock<IChecklistProgressRepository>();
        progressRepo
            .Setup(x => x.SaveCompletedTaskIdsAsync(checklistId, ownerId, It.IsAny<IReadOnlyList<Guid>>(), cancellationToken))
            .Callback<Guid, string, IReadOnlyList<Guid>, CancellationToken>((_, _, ids, _) => capturedIds = ids)
            .Returns(Task.CompletedTask);

        var logger = new Mock<ILogger<SaveChecklistProgressCommandHandler>>();
        var sut = new SaveChecklistProgressCommandHandler(readOnlyRepo.Object, progressRepo.Object, logger.Object);

        var result = await sut.HandleAsync(
            new SaveChecklistProgressCommand(checklistId, ownerId, [validTaskId1, validTaskId1, invalidTaskId, validTaskId2]),
            cancellationToken);

        Assert.True(result.Succeeded);

        progressRepo.Verify(
            x => x.SaveCompletedTaskIdsAsync(checklistId, ownerId, It.IsAny<IReadOnlyList<Guid>>(), cancellationToken),
            Times.Once);

        Assert.NotNull(capturedIds);
        Assert.Equal(2, capturedIds!.Count);
        Assert.Contains(validTaskId1, capturedIds);
        Assert.Contains(validTaskId2, capturedIds);
        Assert.DoesNotContain(invalidTaskId, capturedIds);
    }

    private static Checklist CreateChecklist(
        Guid checklistId,
        string ownerId,
        Guid? taskId1 = null,
        Guid? taskId2 = null)
    {
        return new Checklist
        {
            Id = checklistId,
            UserId = ownerId,
            Sections =
            [
                new Section
                {
                    Id = Guid.NewGuid(),
                    Name = "Section",
                    Position = 1,
                    Tasks =
                    [
                        new TaskItem { Id = taskId1 ?? Guid.NewGuid(), Content = "Task 1", Position = 1 },
                        new TaskItem { Id = taskId2 ?? Guid.NewGuid(), Content = "Task 2", Position = 2 }
                    ]
                }
            ]
        };
    }
}
