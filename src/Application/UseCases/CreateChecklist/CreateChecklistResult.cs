namespace Application.UseCases.CreateChecklist;

public class CreateChecklistResult
{
    public bool Succeeded { get; init; }

    public Guid? Id { get; init; }

    public static CreateChecklistResult Success(Guid id) => new() { Succeeded = true, Id = id };

    public static CreateChecklistResult Failure() => new() { Succeeded = false };
}
