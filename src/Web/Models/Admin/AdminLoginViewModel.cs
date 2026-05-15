using System.ComponentModel.DataAnnotations;

namespace Web.Models.Admin;

public sealed class AdminLoginViewModel
{
    [Required(ErrorMessage = "Email or Username is required")]
    [Display(Name = "Email or Username")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;
}
