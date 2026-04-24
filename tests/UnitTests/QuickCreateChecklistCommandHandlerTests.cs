using Application.Interfaces;
using Application.Options;
using Application.UseCases.QuickCreateChecklist;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace UnitTests;

public class QuickCreateChecklistCommandHandlerTests
{
    private static readonly int[] ExpectedSequentialPositions = [0, 1, 2];

    private readonly Mock<IChecklistRepository> _repositoryMock;
    private readonly Mock<ILogger<QuickCreateChecklistCommandHandler>> _loggerMock;
    private readonly QuickCreateChecklistCommandHandler _handler;

    public QuickCreateChecklistCommandHandlerTests()
    {
        _repositoryMock = new Mock<IChecklistRepository>();
        _loggerMock = new Mock<ILogger<QuickCreateChecklistCommandHandler>>();
        var options = Options.Create(new ChecklistOptions());
        _handler = new QuickCreateChecklistCommandHandler(_repositoryMock.Object, options, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidInput_PersistsChecklistAsPublished()
    {
        var userId = "user-1";
        var input = "# Trip\nTwo-day plan.\n## Day 1\n- [ ] Pack bag\n- [ ] Book hotel";
        var command = new QuickCreateChecklistCommand(input);

        var result = await _handler.HandleAsync(command, userId);

        Assert.True(result.Succeeded);
        Assert.NotEqual(Guid.Empty, result.Value);
        _repositoryMock.Verify(
            r => r.AddAsync(It.Is<Checklist>(c =>
                c.Title == "Trip" &&
                c.Description == "Two-day plan." &&
                c.UserId == userId &&
                c.Status == ChecklistStatus.Published &&
                c.Sections.Count == 1 &&
                c.Sections[0].Name == "Day 1" &&
                c.Sections[0].Tasks.Count == 2)),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AssignsSequentialTaskPositions()
    {
        var input = "# Title\n## Section\n- [ ] A\n- [ ] B\n- [ ] C";
        Checklist? captured = null;
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Checklist>()))
            .Callback<Checklist>(c => captured = c);

        await _handler.HandleAsync(new QuickCreateChecklistCommand(input), "user-1");

        Assert.NotNull(captured);
        var tasks = captured!.Sections[0].Tasks;
        Assert.Equal(ExpectedSequentialPositions, tasks.Select(t => t.Position));
    }

    [Fact]
    public async Task HandleAsync_ParserFails_ReturnsParserErrorWithoutPersisting()
    {
        var command = new QuickCreateChecklistCommand("- [ ] no title");

        var result = await _handler.HandleAsync(command, "user-1");

        Assert.False(result.Succeeded);
        Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Checklist>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NoTasks_ReturnsNoTasksError()
    {
        var command = new QuickCreateChecklistCommand("# Title only");

        var result = await _handler.HandleAsync(command, "user-1");

        Assert.False(result.Succeeded);
        Assert.Equal(QuickCreateErrors.NoTasks, result.ErrorMessage);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Checklist>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_TitleExceedsLimit_ReturnsTitleTooLongError()
    {
        var options = Options.Create(new ChecklistOptions { TitleMaxLength = 5 });
        var handler = new QuickCreateChecklistCommandHandler(_repositoryMock.Object, options, _loggerMock.Object);
        var command = new QuickCreateChecklistCommand("# This is way too long\n- [ ] Task");

        var result = await handler.HandleAsync(command, "user-1");

        Assert.False(result.Succeeded);
        Assert.Contains("Title is too long", result.ErrorMessage!, StringComparison.Ordinal);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Checklist>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_SectionNameExceedsLimit_ReturnsSectionTooLongError()
    {
        var options = Options.Create(new ChecklistOptions { SectionNameMaxLength = 5 });
        var handler = new QuickCreateChecklistCommandHandler(_repositoryMock.Object, options, _loggerMock.Object);
        var command = new QuickCreateChecklistCommand("# Title\n## Way too long section name\n- [ ] Task");

        var result = await handler.HandleAsync(command, "user-1");

        Assert.False(result.Succeeded);
        Assert.Contains("Section name is too long", result.ErrorMessage!, StringComparison.Ordinal);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Checklist>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_RepositoryThrows_PropagatesException()
    {
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Checklist>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));
        var command = new QuickCreateChecklistCommand("# Title\n- [ ] Task");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.HandleAsync(command, "user-1"));
    }
}
