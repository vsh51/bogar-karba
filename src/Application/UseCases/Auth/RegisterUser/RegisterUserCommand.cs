namespace Application.UseCases.Auth.RegisterUser;

public sealed record RegisterUserCommand(string Name, string Surname, string Email, string Password);
