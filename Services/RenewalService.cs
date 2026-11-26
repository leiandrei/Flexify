using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GymCrudApp.Data;
using GymCrudApp.Models;
using GymCrudApp.ViewModels.Renewals;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GymCrudApp.Services
{
    public class RenewalService : IRenewalService
    {
        private readonly GymDbContext _context;

        public RenewalService(GymDbContext context)
        {
            _context = context;
        }

        public async Task<List<ExpiringMemberItemViewModel>> GetExpiringSoonMembersAsync(
            int withinDays = 7,
            bool includeRecentlyExpired = true,
            DateTime? asOfDate = null)
        {
            var today = (asOfDate ?? DateTime.Today).Date;
            var cutoffDate = today.AddDays(withinDays);

            var activeMembers = await _context.Members
                .AsNoTracking()
                .Where(m => m.isActive)
                .ToListAsync();

            var memberships = await _context.Memberships
                .AsNoTracking()
                .Include(ms => ms.Plan)
                .Where(ms => ms.isActive)
                .ToListAsync();

            var result = new List<ExpiringMemberItemViewModel>();

            foreach (var member in activeMembers)
            {
                var latest = memberships
                    .Where(ms => ms.memberId == member.memberId)
                    .OrderByDescending(ms => ms.endDate)
                    .FirstOrDefault();

                if (latest == null) continue;

                var daysRemaining = (latest.endDate.Date - today).Days;
                var isExpired = daysRemaining < 0;
                var isExpiringSoon = daysRemaining >= 0 && latest.endDate.Date <= cutoffDate;

                if (isExpiringSoon || (includeRecentlyExpired && isExpired))
                {
                    result.Add(new ExpiringMemberItemViewModel
                    {
                        MemberId = member.memberId,
                        FullName = member.fullName,
                        Email = member.userEmail,
                        ContactNumber = member.contactNumber,
                        CurrentMembershipId = latest.membershipId,
                        CurrentPlanId = latest.planId,
                        CurrentPlanName = latest.Plan?.planName ?? "Unknown Plan",
                        CurrentPlanPrice = latest.Plan?.planPrice ?? 0m,
                        CurrentEndDate = latest.endDate.Date,
                        DaysRemaining = Math.Max(0, daysRemaining),
                        Status = isExpired ? "Expired" : "Expiring"
                    });
                }
            }

            return result
                .OrderBy(r => r.CurrentEndDate)
                .ThenBy(r => r.FullName)
                .ToList();
        }

        public async Task<OneClickRenewalViewModel?> PrepareRenewalFormAsync(
            int memberId,
            int? overridePlanId = null,
            DateTime? asOfDate = null)
        {
            var today = (asOfDate ?? DateTime.Today).Date;

            var member = await _context.Members
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.memberId == memberId && m.isActive);

            if (member == null) return null;

            var activePlans = await _context.MembershipPlans
                .AsNoTracking()
                .Where(p => p.isActive)
                .OrderBy(p => p.planPrice)
                .ToListAsync();

            if (!activePlans.Any()) return null;

            var latestMembership = await _context.Memberships
                .AsNoTracking()
                .Include(ms => ms.Plan)
                .Where(ms => ms.memberId == memberId && ms.isActive)
                .OrderByDescending(ms => ms.endDate)
                .FirstOrDefaultAsync();

            var selectedPlanId = overridePlanId
                ?? (latestMembership != null && activePlans.Any(p => p.planId == latestMembership.planId)
                    ? latestMembership.planId
                    : activePlans.First().planId);

            var selectedPlan = activePlans.First(p => p.planId == selectedPlanId);
            var (startDate, endDate) = CalculateRenewalDates(latestMembership, selectedPlan.durationDays, today);
            var status = ComputeStatus(latestMembership, today);

            return new OneClickRenewalViewModel
            {
                MemberId = member.memberId,
                MemberName = member.fullName,
                CurrentPlanName = latestMembership?.Plan?.planName,
                CurrentEndDate = latestMembership?.endDate.Date,
                CurrentStatus = status,
                PlanId = selectedPlan.planId,
                Amount = selectedPlan.planPrice,
                PaymentType = "Cash",
                ProjectedStartDate = startDate,
                ProjectedEndDate = endDate,
                AvailablePlans = activePlans.Select(p => new SelectListItem
                {
                    Value = p.planId.ToString(),
                    Text = $"{p.planName} ({p.durationDays} days - ₱{p.planPrice:N0})",
                    Selected = p.planId == selectedPlan.planId
                }).ToList(),
                PlanPrices = activePlans.ToDictionary(p => p.planId, p => p.planPrice)
            };
        }

        public async Task<RenewalResult> RenewMembershipAsync(
            OneClickRenewalViewModel model,
            int recordedByStaffId,
            DateTime? asOfDate = null)
        {
            var today = (asOfDate ?? DateTime.Today).Date;

            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.memberId == model.MemberId && m.isActive);
            if (member == null)
            {
                return new RenewalResult { Succeeded = false, ErrorMessage = "Active member not found." };
            }

            var plan = await _context.MembershipPlans
                .FirstOrDefaultAsync(p => p.planId == model.PlanId && p.isActive);
            if (plan == null)
            {
                return new RenewalResult { Succeeded = false, ErrorMessage = "Selected membership plan is unavailable." };
            }

            if (model.Amount <= 0)
            {
                return new RenewalResult { Succeeded = false, ErrorMessage = "Payment amount must be greater than zero." };
            }

            if (!string.Equals(model.PaymentType, "Cash", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(model.ReferenceNumber))
            {
                return new RenewalResult
                {
                    Succeeded = false,
                    ErrorMessage = "Reference number is required for non-cash payments."
                };
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var currentActiveMemberships = await _context.Memberships
                    .Where(ms => ms.memberId == member.memberId && ms.isActive)
                    .OrderByDescending(ms => ms.endDate)
                    .ToListAsync();

                var latestActive = currentActiveMemberships.FirstOrDefault();
                var (newStartDate, newEndDate) = CalculateRenewalDates(latestActive, plan.durationDays, today);

                // Deactivate superseded memberships so only the renewed subscription remains active
                foreach (var old in currentActiveMemberships)
                {
                    old.isActive = false;
                }

                var newMembership = new Memberships
                {
                    memberId = member.memberId,
                    planId = plan.planId,
                    startDate = newStartDate,
                    endDate = newEndDate,
                    isActive = true
                };

                _context.Memberships.Add(newMembership);
                await _context.SaveChangesAsync();

                var newPayment = new Payments
                {
                    memberId = member.memberId,
                    membershipId = newMembership.membershipId,
                    amount = model.Amount,
                    paymentType = model.PaymentType.Trim(),
                    referenceNumber = string.IsNullOrWhiteSpace(model.ReferenceNumber)
                        ? null
                        : model.ReferenceNumber.Trim(),
                    paymentDate = DateTime.Now,
                    recordedByStaffId = recordedByStaffId,
                    isVoided = false
                };

                _context.Payments.Add(newPayment);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new RenewalResult
                {
                    Succeeded = true,
                    MemberName = member.fullName,
                    NewMembershipId = newMembership.membershipId,
                    NewPaymentId = newPayment.paymentId,
                    NewStartDate = newStartDate,
                    NewEndDate = newEndDate
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new RenewalResult
                {
                    Succeeded = false,
                    ErrorMessage = $"Renewal failed and was rolled back: {ex.Message}"
                };
            }
        }

        public async Task<int> DeactivateExpiredMembershipsAsync(DateTime? asOfDate = null)
        {
            var today = (asOfDate ?? DateTime.Today).Date;

            var expiredList = await _context.Memberships
                .Where(ms => ms.isActive && ms.endDate < today)
                .ToListAsync();

            if (!expiredList.Any()) return 0;

            foreach (var ms in expiredList)
            {
                ms.isActive = false;
            }

            await _context.SaveChangesAsync();
            return expiredList.Count;
        }

        public static (DateTime StartDate, DateTime EndDate) CalculateRenewalDates(
            Memberships? currentMembership,
            int durationDays,
            DateTime today)
        {
            var baseToday = today.Date;
            var startDate = (currentMembership != null &&
                             currentMembership.isActive &&
                             currentMembership.endDate.Date >= baseToday)
                ? currentMembership.endDate.Date.AddDays(1)
                : baseToday;

            var endDate = startDate.AddDays(durationDays);
            return (startDate, endDate);
        }

        public static string ComputeStatus(Memberships? membership, DateTime today)
        {
            if (membership == null) return "No Membership";
            if (!membership.isActive) return "Inactive";

            var daysLeft = (membership.endDate.Date - today.Date).Days;
            if (daysLeft < 0) return "Expired";
            if (daysLeft <= 7) return "Expiring";
            return "Active";
        }
        
        public async Task<int?> GetStaffIdByUserIdAsync(int userId)
        {
            return await _context.Staff
                .AsNoTracking()
                .Where(s => s.userId == userId)
                .Select(s => (int?)s.StaffID)
                .FirstOrDefaultAsync();
        }

        public async Task<int?> GetFallbackStaffIdAsync()
        {
            return await _context.Staff
                .AsNoTracking()
                .Where(s => s.User == null || s.User.isActive)
                .OrderBy(s => s.StaffID)
                .Select(s => (int?)s.StaffID)
                .FirstOrDefaultAsync();
        }
    }
}