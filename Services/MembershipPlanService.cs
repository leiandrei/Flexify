using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GymCrudApp.Data;
using GymCrudApp.Models;
using GymCrudApp.ViewModels.MembershipPlans;
using Microsoft.EntityFrameworkCore;

namespace GymCrudApp.Services
{
    public class MembershipPlanService : IMembershipPlanService
    {
        private readonly GymDbContext _context;

        public MembershipPlanService(GymDbContext context)
        {
            _context = context;
        }

        public async Task<List<MembershipPlanViewModel>> GetAllAsync()
        {
            return await _context.MembershipPlans
                .AsNoTracking()
                .OrderByDescending(p => p.isActive)
                .ThenBy(p => p.planPrice)
                .Select(p => new MembershipPlanViewModel
                {
                    PlanId = p.planId,
                    PlanName = p.planName,
                    PlanPrice = p.planPrice,
                    DurationDays = p.durationDays,
                    PlanDescription = p.planDescription,
                    IsActive = p.isActive
                })
                .ToListAsync();
        }

        public async Task<MembershipPlanViewModel?> GetByIdAsync(int id)
        {
            return await _context.MembershipPlans
                .AsNoTracking()
                .Where(p => p.planId == id)
                .Select(p => new MembershipPlanViewModel
                {
                    PlanId = p.planId,
                    PlanName = p.planName,
                    PlanPrice = p.planPrice,
                    DurationDays = p.durationDays,
                    PlanDescription = p.planDescription,
                    IsActive = p.isActive
                })
                .FirstOrDefaultAsync();
        }

        public async Task<MembershipPlanFormViewModel?> GetForEditAsync(int id)
        {
            return await _context.MembershipPlans
                .AsNoTracking()
                .Where(p => p.planId == id)
                .Select(p => new MembershipPlanFormViewModel
                {
                    PlanId = p.planId,
                    PlanName = p.planName,
                    PlanPrice = p.planPrice,
                    DurationDays = p.durationDays,
                    PlanDescription = p.planDescription,
                    IsActive = p.isActive
                })
                .FirstOrDefaultAsync();
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateAsync(MembershipPlanFormViewModel model)
        {
            var trimmedName = model.PlanName.Trim();
            var exists = await _context.MembershipPlans
                .AnyAsync(p => p.planName.ToLower() == trimmedName.ToLower());

            if (exists)
            {
                return (false, "A membership plan with this name already exists.");
            }

            var plan = new MembershipPlans
            {
                planName = trimmedName,
                planPrice = model.PlanPrice,
                durationDays = model.DurationDays,
                planDescription = model.PlanDescription.Trim(),
                isActive = model.IsActive
            };

            _context.MembershipPlans.Add(plan);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(MembershipPlanFormViewModel model)
        {
            var plan = await _context.MembershipPlans.FirstOrDefaultAsync(p => p.planId == model.PlanId);
            if (plan == null)
            {
                return (false, "Membership plan not found.");
            }

            var trimmedName = model.PlanName.Trim();
            var duplicateName = await _context.MembershipPlans
                .AnyAsync(p => p.planId != model.PlanId && p.planName.ToLower() == trimmedName.ToLower());

            if (duplicateName)
            {
                return (false, "Another membership plan already uses this name.");
            }

            plan.planName = trimmedName;
            plan.planPrice = model.PlanPrice;
            plan.durationDays = model.DurationDays;
            plan.planDescription = model.PlanDescription.Trim();
            plan.isActive = model.IsActive;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id)
        {
            var plan = await _context.MembershipPlans.FirstOrDefaultAsync(p => p.planId == id);
            if (plan == null)
            {
                return (false, "Membership plan not found.");
            }

            var hasMemberships = await _context.Memberships.AnyAsync(m => m.planId == id);
            if (hasMemberships)
            {
                return (false, "Cannot delete this plan because it is linked to existing memberships. Uncheck 'Active Plan' in Edit instead.");
            }

            _context.MembershipPlans.Remove(plan);
            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}