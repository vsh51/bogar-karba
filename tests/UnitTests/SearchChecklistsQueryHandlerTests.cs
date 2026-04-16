using Application.Interfaces;
using Application.UseCases.SearchChecklists;
using Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests;

public class SearchChecklistsQueryHandlerTests
{
    [Fact]
    public async Task HandleWithSearchTermFiltersByTitleOrDescriptionIgnoringCase()
    {
        var items = new[]
        {
            new Checklist { Title = "Deploy", Description = "Deploy to staging" },
            new Checklist { Title = "Review", Description = "Code review" },
            new Checklist { Title = "Fix", Description = "Fix bug" },
        };

        var handler = new SearchChecklistsQueryHandler(
            new FakeChecklistRepository(items),
            NullLogger<SearchChecklistsQueryHandler>.Instance);

        var result = await handler.HandleAsync(new SearchChecklistsQuery("deploy"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        Assert.Equal("Deploy", result.Value[0].Title);
    }

    [Fact]
    public async Task HandleWithEmptySearchTermReturnsAllItems()
    {
        var items = new[]
        {
            new Checklist { Title = "A", Description = "a" },
            new Checklist { Title = "B", Description = "b" },
        };

        var handler = new SearchChecklistsQueryHandler(
            new FakeChecklistRepository(items),
            NullLogger<SearchChecklistsQueryHandler>.Instance);

        var result = await handler.HandleAsync(new SearchChecklistsQuery(null));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task HandleWithSearchTerm_WhenNoMatch_ReturnsEmptyList()
    {
        var items = new[]
        {
            new Checklist { Title = "Apple", Description = "Fruit" },
            new Checklist { Title = "Banana", Description = "Yellow" },
        };

        var handler = new SearchChecklistsQueryHandler(
            new FakeChecklistRepository(items),
            NullLogger<SearchChecklistsQueryHandler>.Instance);

        var result = await handler.HandleAsync(new SearchChecklistsQuery("Orange"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task HandleWithWhitespaceSearchTerm_ReturnsAllItems()
    {
        var items = new[]
        {
            new Checklist { Title = "A", Description = "a" },
        };

        var handler = new SearchChecklistsQueryHandler(
            new FakeChecklistRepository(items),
            NullLogger<SearchChecklistsQueryHandler>.Instance);

        var result = await handler.HandleAsync(new SearchChecklistsQuery("   "));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
    }

    private sealed class FakeChecklistRepository : IChecklistRepository
    {
        private readonly List<Checklist> _items;

        public FakeChecklistRepository(IEnumerable<Checklist> items)
        {
            _items = items.ToList();
        }

        public Task<List<Checklist>> GetAllAsync() => Task.FromResult(_items);

        public Task<IEnumerable<Checklist>> GetByUserIdAsync(string userId)
        {
            return Task.FromResult<IEnumerable<Checklist>>(_items.Where(c => c.UserId == userId));
        }

        public Task<List<Checklist>> GetByIdsAsync(IEnumerable<Guid> ids)
        {
            var idSet = ids.ToHashSet();
            return Task.FromResult(_items.Where(c => idSet.Contains(c.Id)).ToList());
        }

        public Task AddAsync(Checklist checklist)
        {
            _items.Add(checklist);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            _items.RemoveAll(c => c.Id == id);
            return Task.CompletedTask;
        }

        public Task UpdateStatusAsync(Guid id, ChecklistStatus newStatus)
        {
            var item = _items.FirstOrDefault(c => c.Id == id);
            if (item != null)
            {
                item.Status = newStatus;
            }

            return Task.CompletedTask;
        }

        public Task<int> GetTotalCountAsync()
        {
            return Task.FromResult(_items.Count);
        }

        public Task<Checklist?> GetByIdWithDetailsAsync(Guid id)
        {
            return Task.FromResult(_items.FirstOrDefault(c => c.Id == id));
        }

        public Task UpdateAsync()
        {
            return Task.CompletedTask;
        }
    }
}
