using Application.Enums;
using Application.Interfaces;
using Application.Options;
using Application.UseCases.SearchChecklists;
using Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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
            new FakeUserRepository(),
            Options.Create(new ChecklistOptions()),
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
            new FakeUserRepository(),
            Options.Create(new ChecklistOptions()),
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
            new FakeUserRepository(),
            Options.Create(new ChecklistOptions()),
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
            new FakeUserRepository(),
            Options.Create(new ChecklistOptions()),
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

        public Task<int> GetCountByStatusAsync(ChecklistStatus status)
        {
            return Task.FromResult(_items.Count(c => c.Status == status));
        }

        public Task<Checklist?> GetByIdWithDetailsAsync(Guid id)
        {
            return Task.FromResult(_items.FirstOrDefault(c => c.Id == id));
        }

        public Task AddSectionAsync(Section section) => Task.CompletedTask;

        public Task AddTaskAsync(TaskItem task) => Task.CompletedTask;

        public Task UpdateAsync()
        {
            return Task.CompletedTask;
        }

        public Task UpdateVisibilityAsync(Guid id, bool isPublic)
        {
            var item = _items.FirstOrDefault(c => c.Id == id);
            if (item != null)
            {
                item.IsPublic = isPublic;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Task<bool> UserExistsAsync(string identifier, UserLookupMode lookupMode) => Task.FromResult(false);

        public Task<bool> CheckPasswordAsync(string identifier, string password, UserLookupMode lookupMode) => Task.FromResult(false);

        public Task<bool> IsActiveAsync(string identifier, UserLookupMode lookupMode) => Task.FromResult(false);

        public Task<IList<string>> GetRolesAsync(string identifier, UserLookupMode lookupMode) => Task.FromResult<IList<string>>(new List<string>());

        public Task<(bool Succeeded, IEnumerable<string> Errors)> CreateUserAsync(string name, string surname, string email, string password, UserStatus accountStatus) => Task.FromResult((false, Enumerable.Empty<string>()));

        public Task<bool> BanUserAsync(string userId) => Task.FromResult(false);

        public Task<int> GetTotalCountAsync() => Task.FromResult(0);

        public Task<Dictionary<string, string>> GetUsernamesByIdsAsync(IEnumerable<string> userIds) => Task.FromResult(new Dictionary<string, string>());
    }
}
