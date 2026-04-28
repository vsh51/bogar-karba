using Application.DTOs;

namespace Application.Interfaces;

public interface IBoredApiClient
{
    Task<BoredActivityDto?> GetRandomActivityAsync(CancellationToken cancellationToken = default);
}
