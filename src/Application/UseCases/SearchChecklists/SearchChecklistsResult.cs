using Domain.Entities;

namespace Application.UseCases.SearchChecklists;

public sealed record SearchChecklistsResult(List<Checklist> Checklists);
