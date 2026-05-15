using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.ExternalServices;

public sealed class BoredApiClient(
    HttpClient httpClient,
    ILogger<BoredApiClient> logger) : IBoredApiClient
{
    public async Task<BoredActivityDto?> GetRandomActivityAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("random", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Bored API returned {StatusCode}", response.StatusCode);
            return null;
        }

        var data = await response.Content.ReadFromJsonAsync<BoredApiResponse>(
            JsonSerializerOptions.Web, cancellationToken);

        if (data is null)
        {
            return null;
        }

        return new BoredActivityDto
        {
            Activity = data.Activity,
            Link = string.IsNullOrWhiteSpace(data.Link) ? null : data.Link
        };
    }

    private sealed record BoredApiResponse(string Activity, string? Link);
}
