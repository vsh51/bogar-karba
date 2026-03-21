namespace Application.UseCases.DeleteChecklist;

public class DeleteChecklistResult
{
    public bool Succeeded { get; init; }

    public string? ErrorMessage { get; init; }

    public static DeleteChecklistResult Success() => new() { Succeeded = true };

    public static DeleteChecklistResult Failure(string errorMessage) =>
        new() { Succeeded = false, ErrorMessage = errorMessage };
}
