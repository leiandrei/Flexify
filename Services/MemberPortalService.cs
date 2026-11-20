using System;
using System.Linq;
using System.Threading.Tasks;
using GymCrudApp.Data;
using GymCrudApp.Models;
using GymCrudApp.ViewModels.MemberPortal;
using Microsoft.EntityFrameworkCore;

namespace GymCrudApp.Services
{
    public class MemberPortalService : IMemberPortalService
    {
        private readonly GymDbContext _context;

        public MemberPortalService(GymDbContext context)
        {
            _context = context;
        }

        public async Task<MemberPortalViewModel?> GetPortalForUserAsync(
            int? authenticatedUserId,
            string? authenticatedEmail,
            DateTime? asOfDate = null)
        {
            var member = await ResolveAuthenticatedMemberAsync(authenticatedUserId, authenticatedEmail);
            if (member == null)
            {
                return null;
            }

            return await BuildPortalViewModelAsync(member, (asOfDate ?? DateTime.Today).Date);
        }

        public async Task<(bool Authorized, MemberPortalViewModel? Data)> GetPortalForMemberIdAsync(
            int requestedMemberId,
            int? authenticatedUserId,
            string? authenticatedEmail,
            DateTime? asOfDate = null)
        {
            var member = await ResolveAuthenticatedMemberAsync(authenticatedUserId, authenticatedEmail);
            if (member == null || member.memberId != requestedMemberId)
            {
                return (false, null);
            }

            var vm = await BuildPortalViewModelAsync(member, (asOfDate ?? DateTime.Today).Date);
            return (true, vm);
        }

        private async Task<Members?> ResolveAuthenticatedMemberAsync(int? userId, string? email)
        {
            if (userId.HasValue && userId.Value > 0)
            {
                var byUserId = await _context.Members
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.userId == userId.Value && m.isActive);

                if (byUserId != null)
                {
                    return byUserId;
                }
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                var normalized = email.Trim();
                return await _context.Members
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.userEmail == normalized && m.isActive);
            }

            return null;
        }

        private async Task<MemberPortalViewModel> BuildPortalViewModelAsync(Members member, DateTime today)
        {
            var latestMembership = await _context.Memberships
                .AsNoTracking()
                .Include(ms => ms.Plan)
                .Where(ms => ms.memberId == member.memberId)
                .OrderByDescending(ms => ms.isActive)
                .ThenByDescending(ms => ms.endDate)
                .FirstOrDefaultAsync();

            var (status, daysRemaining) = ComputeMembershipStatus(latestMembership, today);

            var payments = await _context.Payments
                .AsNoTracking()
                .Include(p => p.Membership!)
                    .ThenInclude(ms => ms.Plan)
                .Where(p => p.memberId == member.memberId)
                .OrderByDescending(p => p.paymentDate)
                .ThenByDescending(p => p.paymentId)
                .Select(p => new MemberPaymentHistoryItemViewModel
                {
                    PaymentId = p.paymentId,
                    PlanName = p.Membership != null && p.Membership.Plan != null
                        ? p.Membership.Plan.planName
                        : "Unknown Plan",
                    Amount = p.amount,
                    PaymentType = p.paymentType,
                    ReferenceNumber = p.referenceNumber,
                    PaymentDate = p.paymentDate,
                    IsVoided = p.isVoided
                })
                .ToListAsync();

            var attendanceLogs = await _context.Attendance
                .AsNoTracking()
                .Where(a => a.memberId == member.memberId)
                .OrderByDescending(a => a.checkIn)
                .Select(a => new MemberAttendanceHistoryItemViewModel
                {
                    AttendanceId = a.attendanceId,
                    CheckIn = a.checkIn,
                    CheckOut = a.checkOut
                })
                .ToListAsync();

            return new MemberPortalViewModel
            {
                MemberId = member.memberId,
                FullName = member.fullName,
                Email = member.userEmail,
                ContactNumber = member.contactNumber,
                PlanName = latestMembership?.Plan?.planName,
                MembershipStatus = status,
                DaysRemaining = daysRemaining,
                StartDate = latestMembership?.startDate,
                ExpiryDate = latestMembership?.endDate,
                Payments = payments,
                AttendanceLogs = attendanceLogs
            };
        }

        private static (string Status, int DaysRemaining) ComputeMembershipStatus(Memberships? membership, DateTime today)
        {
            if (membership == null) return ("No Membership", 0);
            if (!membership.isActive) return ("Inactive", 0);

            var daysLeft = (membership.endDate.Date - today).Days;
            if (daysLeft < 0) return ("Expired", 0);
            if (daysLeft <= 7) return ("Expiring", daysLeft);
            return ("Active", daysLeft);
        }
    }
}