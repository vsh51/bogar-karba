namespace Application.UseCases.GroupTasksIntoSection;

public record GroupTasksIntoSectionCommand(
    Guid ChecklistId,
    string OwnerId,
    string SectionName,
    List<Guid> TaskIds);
