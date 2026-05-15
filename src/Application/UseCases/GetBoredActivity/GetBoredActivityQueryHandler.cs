using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.GetBoredActivity;

public sealed class GetBoredActivityQueryHandler(
    IBoredApiClient boredApiClient,
    ILogger<GetBoredActivityQueryHandler> logger)
{
    public async Task<Result<BoredActivityDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching random activity from Bored API");

        var activity = await boredApiClient.GetRandomActivityAsync(cancellationToken);

        if (activity is null)
        {
            logger.LogWarning("Bored API returned no activity");
            return ResultErrors.BoredApiUnavailable;
        }

        logger.LogInformation("Fetched activity: {Activity}", activity.Activity);
        return activity;
    }
}
