using Application.Common.Interfaces;
using Application.Services;
using Domain.Entities;
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
    public async Task GetAllChecklistsShouldReturnListOfChecklistsWhenDataExists()
    {
        var expected = new List<Checklist> 
        { 
            new Checklist { Id = Guid.NewGuid(), Title = "Test 1" } 
        };
        _repositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(expected);

        var result = await _service.GetAllChecklists();

        Assert.Single(result);
    }

    [Fact]
    public async Task GetAllChecklistsShouldReturnEmptyWhenNoData()
    {
        _repositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Checklist>());
        var result = await _service.GetAllChecklists();
        Assert.Empty(result);
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
                       .ThrowsAsync(new Exception("DB Error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteChecklist(checklistId));
    }

    [Fact]
    public async Task ServiceShouldLogInformationOnEachAction()
    {
        await _service.GetAllChecklists();

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