namespace Web.Models.Checklist;

public class CreateTaskViewModel
{
    public string Content { get; set; } = string.Empty;

    public int Position { get; set; }

    public string? Link { get; set; }
}
