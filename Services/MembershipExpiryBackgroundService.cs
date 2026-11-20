using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GymCrudApp.Services
{
    public class MembershipExpiryBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MembershipExpiryBackgroundService> _logger;
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

        public MembershipExpiryBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<MembershipExpiryBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var renewalService = scope.ServiceProvider.GetRequiredService<IRenewalService>();
                    var deactivatedCount = await renewalService.DeactivateExpiredMembershipsAsync();

                    if (deactivatedCount > 0)
                    {
                        _logger.LogInformation(
                            "MembershipExpiryBackgroundService flagged {Count} expired memberships as inactive.",
                            deactivatedCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while running daily membership expiry check.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }
    }
}