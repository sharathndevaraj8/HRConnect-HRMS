using HRConnect.API.Options;
using HRConnect.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HRConnect.API.BackgroundServices;

public sealed class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RefreshTokenCleanupService> _logger;
    private readonly JwtOptions _jwtOptions;

    public RefreshTokenCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<RefreshTokenCleanupService> logger,
        IOptions<JwtOptions> jwtOptions)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _jwtOptions = jwtOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CleanupAsync(stoppingToken);

        using var timer = new PeriodicTimer(
            TimeSpan.FromHours(_jwtOptions.RefreshTokenCleanupIntervalHours));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CleanupAsync(stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var retentionCutoffUtc = DateTime.UtcNow.AddDays(-_jwtOptions.RefreshTokenRetentionDays);

            var deletedCount = await dbContext.RefreshTokens
                .Where(refreshToken =>
                    refreshToken.ExpiresAtUtc < retentionCutoffUtc ||
                    (refreshToken.RevokedAtUtc != null && refreshToken.RevokedAtUtc < retentionCutoffUtc))
                .ExecuteDeleteAsync(cancellationToken);

            var deletedPasswordResetCount = await dbContext.PasswordResetTokens
                .Where(token =>
                    token.ExpiresAtUtc < retentionCutoffUtc ||
                    (token.UsedAtUtc != null && token.UsedAtUtc < retentionCutoffUtc))
                .ExecuteDeleteAsync(cancellationToken);

            if (deletedCount > 0)
            {
                _logger.LogInformation("Deleted {DeletedCount} old refresh tokens.", deletedCount);
            }

            if (deletedPasswordResetCount > 0)
            {
                _logger.LogInformation("Deleted {DeletedCount} old password-reset tokens.", deletedPasswordResetCount);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh token cleanup failed.");
        }
    }
}
