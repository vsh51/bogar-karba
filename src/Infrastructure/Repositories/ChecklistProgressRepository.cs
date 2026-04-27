using System.Text.Json;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ChecklistProgressRepository(
    ApplicationDbContext dbContext) : IChecklistProgressRepository
{
    public async Task<IReadOnlyList<Guid>> GetCompletedTaskIdsAsync(
        Guid checklistId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var row = await dbContext.Set<ChecklistProgress>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.ChecklistId == checklistId && x.UserId == userId,
                cancellationToken);

        if (row is null || string.IsNullOrWhiteSpace(row.CompletedTaskIdsJson))
        {
            return Array.Empty<Guid>();
        }

        var ids = ParseGuidJsonArray(row.CompletedTaskIdsJson);
        if (ids.Count == 0)
        {
            return Array.Empty<Guid>();
        }

        return ids;
    }

    public async Task SaveCompletedTaskIdsAsync(
        Guid checklistId,
        string userId,
        IReadOnlyList<Guid> completedTaskIds,
        CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Set<ChecklistProgress>()
            .SingleOrDefaultAsync(
                x => x.ChecklistId == checklistId && x.UserId == userId,
                cancellationToken);

        if (existing is null)
        {
            existing = new ChecklistProgress
            {
                ChecklistId = checklistId,
                UserId = userId
            };
            dbContext.Set<ChecklistProgress>().Add(existing);
        }

        existing.CompletedTaskIdsJson = JsonSerializer.Serialize(completedTaskIds.Distinct().ToList());
        existing.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<Guid> ParseGuidJsonArray(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<Guid>();
        }

        var trimmed = json.Trim();
        if (trimmed.Length < 2 || trimmed[0] != '[' || trimmed[^1] != ']')
        {
            return Array.Empty<Guid>();
        }

        var body = trimmed[1..^1].Trim();
        if (body.Length == 0)
        {
            return Array.Empty<Guid>();
        }

        var rawItems = body.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (rawItems.Length == 0)
        {
            return Array.Empty<Guid>();
        }

        var result = new List<Guid>(rawItems.Length);

        foreach (var item in rawItems)
        {
            var token = item.Trim();
            if (token.Length >= 2 && token[0] == '"' && token[^1] == '"')
            {
                token = token[1..^1];
            }

            if (!Guid.TryParse(token, out var guid))
            {
                continue;
            }

            result.Add(guid);
        }

        return result.Distinct().ToList();
    }
}
