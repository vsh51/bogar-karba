namespace Application.UseCases.AdminAuth;

public class AdminLoginResult
{
    public bool Succeeded { get; set; }

    public string? ErrorMessage { get; set; }
}
