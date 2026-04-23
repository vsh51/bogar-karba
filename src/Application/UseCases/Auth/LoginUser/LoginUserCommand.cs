namespace Application.UseCases.Auth.LoginUser;

public sealed record LoginUserCommand(string LoginIdentifier, string Password);
