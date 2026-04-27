namespace Application.Interfaces;

public interface IChecklistProgressRepository
{
    Task<IReadOnlyList<Guid>> GetCompletedTaskIdsAsync(
        Guid checklistId,
        string userId,
        CancellationToken cancellationToken = default);

    Task SaveCompletedTaskIdsAsync(
        Guid checklistId,
        string userId,
        IReadOnlyList<Guid> completedTaskIds,
        CancellationToken cancellationToken = default);
}
