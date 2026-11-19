using System;
using System.Linq;
using System.Threading.Tasks;
using GymCrudApp.Data;
using GymCrudApp.Models;
using GymCrudApp.ViewModels.Attendance;
using Microsoft.EntityFrameworkCore;

namespace GymCrudApp.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly GymDbContext _context;

        public AttendanceService(GymDbContext context)
        {
            _context = context;
        }

        public async Task<AttendanceIndexViewModel> GetIndexViewModelAsync(
            string? searchQuery,
            DateTime? filterDate,
            DateTime? asOfNow = null)
        {
            var now = asOfNow ?? DateTime.Now;
            var today = now.Date;
            var targetDate = (filterDate ?? today).Date;

            var vm = new AttendanceIndexViewModel
            {
                SearchQuery = searchQuery,
                FilterDate = targetDate
            };

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var term = searchQuery.Trim();
                var matchedMembers = await _context.Members
                    .AsNoTracking()
                    .Where(m => m.isActive &&
                                (m.fullName.Contains(term) ||
                                 m.userEmail.Contains(term) ||
                                 m.contactNumber.Contains(term)))
                    .OrderBy(m => m.fullName)
                    .Take(15)
                    .ToListAsync();

                var memberIds = matchedMembers.Select(m => m.memberId).ToList();

                var latestMemberships = await _context.Memberships
                    .AsNoTracking()
                    .Include(ms => ms.Plan)
                    .Where(ms => memberIds.Contains(ms.memberId))
                    .ToListAsync();

                var openTodayMemberIds = await _context.Attendance
                    .AsNoTracking()
                    .Where(a => memberIds.Contains(a.memberId) &&
                                a.checkIn.Date == today &&
                                a.checkOut == null)
                    .Select(a => a.memberId)
                    .Distinct()
                    .ToListAsync();

                foreach (var member in matchedMembers)
                {
                    var latest = latestMemberships
                        .Where(ms => ms.memberId == member.memberId)
                        .OrderByDescending(ms => ms.isActive)
                        .ThenByDescending(ms => ms.endDate)
                        .FirstOrDefault();

                    var status = ComputeMembershipStatus(latest, today);

                    vm.MatchingMembers.Add(new MemberCheckInSearchResultViewModel
                    {
                        MemberId = member.memberId,
                        FullName = member.fullName,
                        Email = member.userEmail,
                        ContactNumber = member.contactNumber,
                        PlanName = latest?.Plan?.planName,
                        ExpiryDate = latest?.endDate,
                        MembershipStatus = status,
                        HasOpenCheckInToday = openTodayMemberIds.Contains(member.memberId)
                    });
                }
            }

            vm.Records = await _context.Attendance
                .AsNoTracking()
                .Include(a => a.Member)
                .Where(a => a.checkIn.Date == targetDate)
                .OrderBy(a => a.checkOut.HasValue) // Open check-ins appear at the top
                .ThenByDescending(a => a.checkIn)
                .Select(a => new AttendanceListItemViewModel
                {
                    AttendanceId = a.attendanceId,
                    MemberId = a.memberId,
                    MemberName = a.Member != null ? a.Member.fullName : "Unknown Member",
                    MemberEmail = a.Member != null ? a.Member.userEmail : string.Empty,
                    CheckIn = a.checkIn,
                    CheckOut = a.checkOut
                })
                .ToListAsync();

            return vm;
        }

        public async Task<(bool Success, string? WarningMessage, string? ErrorMessage)> CheckInAsync(
            int memberId,
            DateTime? checkInTime = null)
        {
            var now = checkInTime ?? DateTime.Now;
            var today = now.Date;

            var member = await _context.Members
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.memberId == memberId);

            if (member == null || !member.isActive)
            {
                return (false, null, "Check-in blocked: Member record does not exist or is inactive.");
            }

            var latestMembership = await _context.Memberships
                .AsNoTracking()
                .Include(ms => ms.Plan)
                .Where(ms => ms.memberId == memberId)
                .OrderByDescending(ms => ms.isActive)
                .ThenByDescending(ms => ms.endDate)
                .FirstOrDefaultAsync();

            var status = ComputeMembershipStatus(latestMembership, today);

            if (status == "No Membership" || status == "Inactive")
            {
                return (false, null, $"Check-in blocked: {member.fullName} does not have an active membership plan.");
            }

            if (status == "Expired")
            {
                return (false, null, $"Check-in blocked: {member.fullName}'s membership expired on {latestMembership!.endDate:yyyy-MM-dd}. Please renew before check-in.");
            }

            var hasOpenCheckInToday = await _context.Attendance
                .AnyAsync(a => a.memberId == memberId &&
                               a.checkIn.Date == today &&
                               a.checkOut == null);

            if (hasOpenCheckInToday)
            {
                return (false, null, $"Check-in blocked: {member.fullName} already has an open check-in today. Check them out first.");
            }

            var attendance = new Attendance
            {
                memberId = memberId,
                checkIn = now,
                checkOut = null
            };

            _context.Attendance.Add(attendance);
            await _context.SaveChangesAsync();

            string? warning = null;
            if (status == "Expiring" && latestMembership != null)
            {
                var daysLeft = (latestMembership.endDate.Date - today).Days;
                warning = $"Warning: {member.fullName}'s membership expires in {daysLeft} day(s) ({latestMembership.endDate:yyyy-MM-dd}).";
            }

            return (true, warning, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> CheckOutAsync(
            int attendanceId,
            DateTime? checkOutTime = null)
        {
            var record = await _context.Attendance
                .FirstOrDefaultAsync(a => a.attendanceId == attendanceId);

            if (record == null)
            {
                return (false, "Attendance record not found.");
            }

            if (record.checkOut.HasValue)
            {
                return (false, "This member has already checked out.");
            }

            var now = checkOutTime ?? DateTime.Now;
            record.checkOut = now <= record.checkIn ? record.checkIn.AddSeconds(1) : now;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<AttendanceListItemViewModel?> GetByIdAsync(int id)
        {
            return await _context.Attendance
                .AsNoTracking()
                .Include(a => a.Member)
                .Where(a => a.attendanceId == id)
                .Select(a => new AttendanceListItemViewModel
                {
                    AttendanceId = a.attendanceId,
                    MemberId = a.memberId,
                    MemberName = a.Member != null ? a.Member.fullName : "Unknown Member",
                    MemberEmail = a.Member != null ? a.Member.userEmail : string.Empty,
                    CheckIn = a.checkIn,
                    CheckOut = a.checkOut
                })
                .FirstOrDefaultAsync();
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id)
        {
            var record = await _context.Attendance.FirstOrDefaultAsync(a => a.attendanceId == id);
            if (record == null)
            {
                return (false, "Attendance record not found.");
            }

            _context.Attendance.Remove(record);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        private static string ComputeMembershipStatus(Memberships? membership, DateTime today)
        {
            if (membership == null) return "No Membership";
            if (!membership.isActive) return "Inactive";

            var daysLeft = (membership.endDate.Date - today).Days;
            if (daysLeft < 0) return "Expired";
            if (daysLeft <= 7) return "Expiring";
            return "Active";
        }
    }
}