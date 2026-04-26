namespace Domain.Entities;

public class TaskItem
{
    public Guid Id { get; set; }

    public string Content { get; set; } = string.Empty;

    public int Position { get; set; }

    public string? Link { get; set; }

    public Guid SectionId { get; set; }

    public Section Section { get; set; } = null!;
}
