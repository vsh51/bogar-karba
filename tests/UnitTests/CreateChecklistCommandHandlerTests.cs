using Application.Interfaces;
using Application.UseCases.CreateChecklist;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class CreateChecklistCommandHandlerTests
{
    private readonly Mock<IChecklistRepository> _repositoryMock;
    private readonly Mock<ILogger<CreateChecklistCommandHandler>> _loggerMock;
    private readonly CreateChecklistCommandHandler _handler;

    public CreateChecklistCommandHandlerTests()
    {
        _repositoryMock = new Mock<IChecklistRepository>();
        _loggerMock = new Mock<ILogger<CreateChecklistCommandHandler>>();
        _handler = new CreateChecklistCommandHandler(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ReturnsSuccessAndCallsRepository()
    {
        var userId = "user-123";
        var tasks = new List<CreateTaskRequest>
        {
            new CreateTaskRequest("Task 1", 0),
        };
        var sections = new List<CreateSectionRequest>
        {
            new CreateSectionRequest("Section 1", 0, tasks),
        };
        var request = new CreateChecklistCommand("Test Checklist", "Test Description", sections);

        var result = await _handler.HandleAsync(request, userId);

        Assert.True(result.Succeeded);
        Assert.NotEqual(Guid.Empty, result.Value);
        _repositoryMock.Verify(
            r => r.AddAsync(It.Is<Checklist>(c =>
            c.Title == request.Title &&
            c.UserId == userId &&
            c.Status == ChecklistStatus.Published &&
            c.Sections.Count == 1)),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_EmptyTitle_ReturnsFailure()
    {
        var request = new CreateChecklistCommand(string.Empty, "Desc", new List<CreateSectionRequest>());

        var result = await _handler.HandleAsync(request, "user-123");

        Assert.False(result.Succeeded);
        Assert.Equal("Title is required.", result.ErrorMessage);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Checklist>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_RepositoryThrows_PropagatesException()
    {
        var request = new CreateChecklistCommand("Valid", "Desc", new List<CreateSectionRequest>());
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Checklist>()))
            .ThrowsAsync(new InvalidOperationException("DB Error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.HandleAsync(request, "user-123"));
    }
}
