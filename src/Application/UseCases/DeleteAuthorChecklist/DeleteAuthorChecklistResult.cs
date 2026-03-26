namespace Application.UseCases.DeleteAuthorChecklist;

public class DeleteAuthorChecklistResult
{
    public bool Succeeded { get; init; }

    public string? ErrorMessage { get; init; }

    public static DeleteAuthorChecklistResult Success() => new() { Succeeded = true };

    public static DeleteAuthorChecklistResult Failure(string errorMessage) =>
        new() { Succeeded = false, ErrorMessage = errorMessage };
}
