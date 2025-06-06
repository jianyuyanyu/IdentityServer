// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.Bff.Configuration;
using Duende.Bff.Otel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Duende.Bff.SessionManagement.SessionStore;

/// <summary>
/// Helper to cleanup expired sessions.
/// </summary>
internal class SessionCleanupHost(
    BffMetrics metrics,
    IServiceProvider serviceProvider,
    IOptions<BffOptions> options,
    ILogger<SessionCleanupHost> logger) : IHostedService
{
    private readonly BffOptions _options = options.Value;

    private TimeSpan CleanupInterval => _options.SessionCleanupInterval;

    private CancellationTokenSource? _source;

    /// <summary>
    /// Starts the token cleanup polling.
    /// </summary>
    public Task StartAsync(CT ct)
    {
        if (_options.EnableSessionCleanup)
        {
            if (_source != null)
            {
                throw new InvalidOperationException("Already started. Call Stop first.");
            }

            if (IsIUserSessionStoreCleanupRegistered())
            {
                logger.LogDebug("Starting BFF session cleanup");

                _source = CancellationTokenSource.CreateLinkedTokenSource(ct);

                Task.Factory.StartNew(() => StartInternalAsync(_source.Token));
            }
            else
            {
                logger.LogWarning("BFF session cleanup is enabled, but no IUserSessionStoreCleanup is registered in DI. BFF session cleanup will not run.");
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the token cleanup polling.
    /// </summary>
    public Task StopAsync(CT ct)
    {
        if (_options.EnableSessionCleanup && _source != null)
        {
            logger.LogDebug("Stopping BFF session cleanup");

            _source.Cancel();
            _source = null;
        }

        return Task.CompletedTask;
    }

    private async Task StartInternalAsync(CT ct)
    {
        while (true)
        {
            if (ct.IsCancellationRequested)
            {
                logger.LogDebug("CancellationRequested. Exiting.");
                break;
            }

            try
            {
                await Task.Delay(CleanupInterval, ct);
            }
            catch (TaskCanceledException)
            {
                logger.LogDebug("TaskCanceledException. Exiting.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError("Task.Delay exception: {0}. Exiting.", ex.Message);
                break;
            }

            if (ct.IsCancellationRequested)
            {
                logger.LogDebug("CancellationRequested. Exiting.");
                break;
            }

            await RunAsync(ct);
        }
    }

    private async Task RunAsync(CT ct = default)
    {
        try
        {
            using var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var tokenCleanupService = serviceScope.ServiceProvider.GetRequiredService<IUserSessionStoreCleanup>();
            var removed = await tokenCleanupService.DeleteExpiredSessionsAsync(ct);
            metrics.SessionsEnded(removed);
        }
        catch (Exception ex)
        {
            logger.LogError("Exception deleting expired sessions: {exception}", ex.Message);
        }
    }

    private bool IsIUserSessionStoreCleanupRegistered()
    {
        var isService = serviceProvider.GetRequiredService<IServiceProviderIsService>();
        return isService.IsService(typeof(IUserSessionStoreCleanup));
    }
}
