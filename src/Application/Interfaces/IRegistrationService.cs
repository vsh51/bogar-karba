using Application.UseCases.Registration;

namespace Application.Interfaces;

public interface IRegistrationService
{
    Task<RegistrationResult> RegisterAsync(string name, string surname, string email, string password);
}
