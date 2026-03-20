namespace Application.UseCases.Auth.LoginUser;

public sealed record LoginUserCommand(string Email, string Password);
