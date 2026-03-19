using Application.Interfaces;
using Application.UseCases;
using Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests;

public class SearchChecklistsServiceTests
{
    [Fact]
    public void ExecuteWithSearchTermFiltersByTitleOrDescriptionIgnoringCase()
    {
        // Arrange
        var items = new[]
        {
            new Checklist { Title = "Deploy", Description = "Deploy to staging" },
            new Checklist { Title = "Review", Description = "Code review" },
            new Checklist { Title = "Fix", Description = "Fix bug" },
        };

        var service = new SearchChecklistsService(
            new FakeChecklistRepository(items),
            NullLogger<SearchChecklistsService>.Instance);

        // Act
        var result = service.Execute("deploy");

        // Assert
        Assert.Single(result);
        Assert.Equal("Deploy", result[0].Title);
    }

    [Fact]
    public void ExecuteWithEmptySearchTermReturnsAllItems()
    {
        // Arrange
        var items = new[]
        {
            new Checklist { Title = "A", Description = "a" },
            new Checklist { Title = "B", Description = "b" },
        };

        var service = new SearchChecklistsService(
            new FakeChecklistRepository(items),
            NullLogger<SearchChecklistsService>.Instance);

        // Act
        var result = service.Execute(null);

        // Assert
        Assert.Equal(2, result.Count);
    }

    private sealed class FakeChecklistRepository : IChecklistRepository
    {
        private readonly List<Checklist> _items;

        public FakeChecklistRepository(IEnumerable<Checklist> items)
        {
            _items = items.ToList();
        }

        public IQueryable<Checklist> GetAll() => _items.AsQueryable();

        public Task DeleteAsync(Guid id)
        {
            _items.RemoveAll(c => c.Id == id);
            return Task.CompletedTask;
        }
    }
}
