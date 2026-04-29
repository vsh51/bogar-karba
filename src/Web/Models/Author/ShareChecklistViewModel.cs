using System.ComponentModel.DataAnnotations;

namespace Web.Models.Author;

public sealed class ShareChecklistViewModel
{
    [Required]
    public string TargetUsername { get; set; } = string.Empty;
}
