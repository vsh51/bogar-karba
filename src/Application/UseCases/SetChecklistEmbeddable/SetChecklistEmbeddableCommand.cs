namespace Application.UseCases.SetChecklistEmbeddable;

public sealed record SetChecklistEmbeddableCommand(Guid Id, bool IsEmbeddable, string? OwnerId = null);
