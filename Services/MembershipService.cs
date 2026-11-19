using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GymCrudApp.Data;
using GymCrudApp.Models;
using GymCrudApp.ViewModels.Memberships;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GymCrudApp.Services
{
    public class MembershipService : IMembershipService
    {
        private readonly GymDbContext _context;

        public MembershipService(GymDbContext context)
        {
            _context = context;
        }

        public DateTime CalculateEndDate(DateTime startDate, int durationDays)
        {
            if (durationDays <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationDays), "Duration must be greater than zero.");
            }

            return startDate.Date.AddDays(durationDays);
        }

        public int CalculateDaysRemaining(DateTime endDate, DateTime? asOfDate = null)
        {
            var today = (asOfDate ?? DateTime.Today).Date;
            var remaining = (endDate.Date - today).Days;
            return remaining < 0 ? 0 : remaining;
        }

        public string ComputeStatus(DateTime endDate, bool isActive = true, DateTime? asOfDate = null)
        {
            if (!isActive)
            {
                return "Inactive";
            }

            var today = (asOfDate ?? DateTime.Today).Date;
            var daysLeft = (endDate.Date - today).Days;

            if (daysLeft < 0)
            {
                return "Expired";
            }

            if (daysLeft <= 7)
            {
                return "Expiring";
            }

            return "Active";
        }

        public async Task<List<MembershipListItemViewModel>> GetAllAsync()
        {
            var today = DateTime.Today;
            var rows = await _context.Memberships
                .AsNoTracking()
                .Include(m => m.Member)
                .Include(m => m.Plan)
                .OrderByDescending(m => m.isActive)
                .ThenBy(m => m.endDate)
                .ToListAsync();

            return rows.Select(m => new MembershipListItemViewModel
            {
                MembershipId = m.membershipId,
                MemberId = m.memberId,
                MemberName = m.Member?.fullName ?? "Unknown Member",
                PlanName = m.Plan?.planName ?? "Unknown Plan",
                StartDate = m.startDate,
                EndDate = m.endDate,
                IsActive = m.isActive,
                DaysRemaining = CalculateDaysRemaining(m.endDate, today),
                ComputedStatus = ComputeStatus(m.endDate, m.isActive, today)
            }).ToList();
        }

        public async Task<MembershipDetailsViewModel?> GetByIdAsync(int id)
        {
            var entity = await _context.Memberships
                .AsNoTracking()
                .Include(m => m.Member)
                .Include(m => m.Plan)
                .FirstOrDefaultAsync(m => m.membershipId == id);

            return entity == null ? null : MapToDetails(entity);
        }

        public async Task<MembershipDetailsViewModel?> GetLatestForMemberAsync(int memberId)
        {
            var entity = await _context.Memberships
                .AsNoTracking()
                .Include(m => m.Member)
                .Include(m => m.Plan)
                .Where(m => m.memberId == memberId)
                .OrderByDescending(m => m.isActive)
                .ThenByDescending(m => m.endDate)
                .FirstOrDefaultAsync();

            return entity == null ? null : MapToDetails(entity);
        }

        public async Task<MembershipCreateViewModel> BuildCreateViewModelAsync(int? preselectedMemberId = null)
        {
            var members = await _context.Members
                .AsNoTracking()
                .Where(m => m.isActive)
                .OrderBy(m => m.fullName)
                .Select(m => new { m.memberId, Display = $"{m.fullName} ({m.userEmail})" })
                .ToListAsync();

            var plans = await _context.MembershipPlans
                .AsNoTracking()
                .Where(p => p.isActive)
                .OrderBy(p => p.planPrice)
                .Select(p => new { p.planId, Display = $"{p.planName} - {p.durationDays} days ({p.planPrice:N2})" })
                .ToListAsync();

            return new MembershipCreateViewModel
            {
                MemberId = preselectedMemberId ?? 0,
                StartDate = DateTime.Today,
                Members = new SelectList(members, "memberId", "Display", preselectedMemberId),
                Plans = new SelectList(plans, "planId", "Display")
            };
        }

        public async Task<MembershipEditViewModel?> GetForEditAsync(int id)
        {
            var entity = await _context.Memberships
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.membershipId == id);

            if (entity == null) return null;

            var vm = new MembershipEditViewModel
            {
                MembershipId = entity.membershipId,
                MemberId = entity.memberId,
                PlanId = entity.planId,
                StartDate = entity.startDate,
                EndDate = entity.endDate,
                IsActive = entity.isActive
            };

            await PopulateSelectListsAsync(vm);
            return vm;
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateAsync(MembershipCreateViewModel model)
        {
            var member = await _context.Members.FirstOrDefaultAsync(m => m.memberId == model.MemberId);
            if (member == null || !member.isActive)
            {
                return (false, "Selected member does not exist or is inactive.");
            }

            var plan = await _context.MembershipPlans.FirstOrDefaultAsync(p => p.planId == model.PlanId);
            if (plan == null || !plan.isActive)
            {
                return (false, "Selected membership plan does not exist or is inactive.");
            }

            var endDate = CalculateEndDate(model.StartDate, plan.durationDays);

            var membership = new Memberships
            {
                memberId = model.MemberId,
                planId = model.PlanId,
                startDate = model.StartDate.Date,
                endDate = endDate,
                isActive = true
            };

            _context.Memberships.Add(membership);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(MembershipEditViewModel model)
        {
            var entity = await _context.Memberships.FirstOrDefaultAsync(m => m.membershipId == model.MembershipId);
            if (entity == null)
            {
                return (false, "Membership record not found.");
            }

            entity.memberId = model.MemberId;
            entity.planId = model.PlanId;
            entity.startDate = model.StartDate.Date;
            entity.endDate = model.EndDate.Date;
            entity.isActive = model.IsActive;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id)
        {
            var entity = await _context.Memberships.FirstOrDefaultAsync(m => m.membershipId == id);
            if (entity == null)
            {
                return (false, "Membership record not found.");
            }

            var hasPayments = await _context.Payments.AnyAsync(p => p.membershipId == id);
            if (hasPayments)
            {
                return (false, "Cannot delete this membership because it has recorded payments. Uncheck 'Active Subscription' in Edit instead.");
            }

            _context.Memberships.Remove(entity);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        private MembershipDetailsViewModel MapToDetails(Memberships entity)
        {
            var today = DateTime.Today;
            return new MembershipDetailsViewModel
            {
                MembershipId = entity.membershipId,
                MemberId = entity.memberId,
                MemberName = entity.Member?.fullName ?? "Unknown Member",
                MemberEmail = entity.Member?.userEmail ?? string.Empty,
                PlanId = entity.planId,
                PlanName = entity.Plan?.planName ?? "Unknown Plan",
                PlanPrice = entity.Plan?.planPrice ?? 0m,
                DurationDays = entity.Plan?.durationDays ?? 0,
                StartDate = entity.startDate,
                EndDate = entity.endDate,
                IsActive = entity.isActive,
                LastReminderSent = entity.lastReminderSent,
                DaysRemaining = CalculateDaysRemaining(entity.endDate, today),
                ComputedStatus = ComputeStatus(entity.endDate, entity.isActive, today)
            };
        }

        private async Task PopulateSelectListsAsync(MembershipEditViewModel vm)
        {
            var members = await _context.Members
                .AsNoTracking()
                .OrderBy(m => m.fullName)
                .Select(m => new { m.memberId, Display = $"{m.fullName} ({m.userEmail})" })
                .ToListAsync();

            var plans = await _context.MembershipPlans
                .AsNoTracking()
                .OrderBy(p => p.planPrice)
                .Select(p => new { p.planId, Display = $"{p.planName} - {p.durationDays} days ({p.planPrice:N2})" })
                .ToListAsync();

            vm.Members = new SelectList(members, "memberId", "Display", vm.MemberId);
            vm.Plans = new SelectList(plans, "planId", "Display", vm.PlanId);
        }
    }
}