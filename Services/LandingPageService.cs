using System.Linq;
using System.Threading.Tasks;
using GymCrudApp.Data;
using GymCrudApp.ViewModels.Home;
using Microsoft.EntityFrameworkCore;

namespace GymCrudApp.Services
{
    public class LandingPageService : ILandingPageService
    {
        private readonly GymDbContext _context;

        public LandingPageService(GymDbContext context)
        {
            _context = context;
        }

        public async Task<LandingPageViewModel> GetLandingPageDataAsync()
        {
            var activePlans = await _context.MembershipPlans
                .AsNoTracking()
                .Where(p => p.isActive)
                .OrderBy(p => p.planPrice)
                .ThenBy(p => p.durationDays)
                .Select(p => new PublicMembershipPlanViewModel
                {
                    PlanId = p.planId,
                    PlanName = p.planName,
                    PlanPrice = p.planPrice,
                    DurationDays = p.durationDays,
                    PlanDescription = p.planDescription
                })
                .ToListAsync();

            return new LandingPageViewModel
            {
                Plans = activePlans
            };
        }
    }
}