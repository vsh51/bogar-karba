namespace Application.UseCases.GetChecklistsByIds;

public sealed class GetChecklistsByIdsQuery(List<Guid> ids)
{
    public List<Guid> Ids { get; } = ids;
}
