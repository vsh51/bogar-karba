using Application.Interfaces;
using Application.UseCases.SearchChecklists;
using Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests;

public class SearchChecklistsQueryHandlerTests
{
    [Fact]
    public void HandleWithSearchTermFiltersByTitleOrDescriptionIgnoringCase()
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

        var result = handler.Handle(new SearchChecklistsQuery("deploy"));

        Assert.Single(result.Checklists);
        Assert.Equal("Deploy", result.Checklists[0].Title);
    }

    [Fact]
    public void HandleWithEmptySearchTermReturnsAllItems()
    {
        var items = new[]
        {
            new Checklist { Title = "A", Description = "a" },
            new Checklist { Title = "B", Description = "b" },
        };

        var handler = new SearchChecklistsQueryHandler(
            new FakeChecklistRepository(items),
            NullLogger<SearchChecklistsQueryHandler>.Instance);

        var result = handler.Handle(new SearchChecklistsQuery(null));

        Assert.Equal(2, result.Checklists.Count);
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
