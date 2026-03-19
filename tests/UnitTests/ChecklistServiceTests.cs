using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests;

public class ChecklistServiceTests
{
    private readonly Mock<IChecklistRepository> _repositoryMock;
    private readonly Mock<ILogger<ChecklistService>> _loggerMock;
    private readonly ChecklistService _service;

    public ChecklistServiceTests()
    {
        _repositoryMock = new Mock<IChecklistRepository>();
        _loggerMock = new Mock<ILogger<ChecklistService>>();
        _service = new ChecklistService(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task DeleteChecklistShouldCallRepositoryWhenIdIsValid()
    {
        var checklistId = Guid.NewGuid();
        await _service.DeleteChecklist(checklistId);
        _repositoryMock.Verify(repo => repo.DeleteAsync(checklistId), Times.Once);
    }

    [Fact]
    public async Task DeleteChecklistShouldThrowExceptionWhenRepositoryFails()
    {
        var checklistId = Guid.NewGuid();
        _repositoryMock.Setup(repo => repo.DeleteAsync(checklistId))
                       .ThrowsAsync(new InvalidOperationException("DB Error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteChecklist(checklistId));
    }

    [Fact]
    public async Task DeleteChecklistShouldLogInformation()
    {
        var checklistId = Guid.NewGuid();
        await _service.DeleteChecklist(checklistId);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
