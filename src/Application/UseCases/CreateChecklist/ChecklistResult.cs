namespace Application.UseCases.CreateChecklist;

public class ChecklistResult
{
    public bool Succeeded { get; init; }

    public string? ErrorMessage { get; init; }

    public Guid? Id { get; init; }

    public static ChecklistResult Success(Guid id) => new() { Succeeded = true, Id = id };

    public static ChecklistResult Failure(string errorMessage) =>
        new() { Succeeded = false, ErrorMessage = errorMessage };
}
