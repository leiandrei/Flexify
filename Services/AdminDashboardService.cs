using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GymCrudApp.Data;
using GymCrudApp.ViewModels.AdminDashboard;
using Microsoft.EntityFrameworkCore;

namespace GymCrudApp.Services
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly GymDbContext _context;

        public AdminDashboardService(GymDbContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardViewModel> GetDashboardMetricsAsync(DateTime? asOfDate = null)
        {
            var today = (asOfDate ?? DateTime.Today).Date;
            var tomorrow = today.AddDays(1);
            var sevenDaysOut = today.AddDays(7);
            var recentlyExpiredFrom = today.AddDays(-7);

            var monthStart = new DateTime(today.Year, today.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            // 1. Active Members (distinct active member accounts with an active, unexpired subscription)
            var activeMembersCount = await _context.Memberships
                .AsNoTracking()
                .Where(ms => ms.isActive &&
                             ms.endDate >= today &&
                             ms.Member != null &&
                             ms.Member.isActive)
                .Select(ms => ms.memberId)
                .Distinct()
                .CountAsync();

            // 2. Expiring in 7 Days (active subscriptions ending between today and today + 7 days inclusive)
            var expiringIn7DaysCount = await _context.Memberships
                .AsNoTracking()
                .Where(ms => ms.isActive &&
                             ms.endDate >= today &&
                             ms.endDate <= sevenDaysOut &&
                             ms.Member != null &&
                             ms.Member.isActive)
                .CountAsync();

            // 3. Revenue This Month (non-voided payments recorded within the current calendar month)
            var revenueThisMonth = await _context.Payments
                .AsNoTracking()
                .Where(p => !p.isVoided &&
                            p.paymentDate >= monthStart &&
                            p.paymentDate < nextMonthStart)
                .SumAsync(p => (decimal?)p.amount) ?? 0m;

            // 4. Check-Ins Today + currently on the floor
            var checkInsTodayCount = await _context.Attendance
                .AsNoTracking()
                .Where(a => a.checkIn >= today && a.checkIn < tomorrow)
                .CountAsync();

            var onFloorCount = await _context.Attendance
                .AsNoTracking()
                .Where(a => a.checkIn >= today && a.checkIn < tomorrow && a.checkOut == null)
                .CountAsync();

            // 5. New Members Joined This Month
            var newMembersThisMonthCount = await _context.Members
                .AsNoTracking()
                .Where(m => m.isActive &&
                            m.joinDate >= monthStart &&
                            m.joinDate < nextMonthStart)
                .CountAsync();

            // 6. Chart Series: Check-Ins Over the Last 7 Days
            var sevenDaysAgo = today.AddDays(-6);
            var recentCheckIns = await _context.Attendance
                .AsNoTracking()
                .Where(a => a.checkIn >= sevenDaysAgo && a.checkIn < tomorrow)
                .Select(a => a.checkIn)
                .ToListAsync();

            var dailyLabels = new List<string>(7);
            var dailyCounts = new List<int>(7);
            for (var d = sevenDaysAgo; d <= today; d = d.AddDays(1))
            {
                var abbr = d.ToString("ddd", System.Globalization.CultureInfo.InvariantCulture);
                dailyLabels.Add(abbr.Length >= 2 ? abbr[..2] : abbr);
                dailyCounts.Add(recentCheckIns.Count(dt => dt.Date == d));
            }

            // 7. Chart Series: Valid Revenue Over the Last 6 Months
            var sixMonthsStart = monthStart.AddMonths(-5);
            var recentPayments = await _context.Payments
                .AsNoTracking()
                .Where(p => !p.isVoided &&
                            p.paymentDate >= sixMonthsStart &&
                            p.paymentDate < nextMonthStart)
                .Select(p => new { p.paymentDate, p.amount })
                .ToListAsync();

            var monthlyLabels = new List<string>(6);
            var monthlyTotals = new List<decimal>(6);
            for (var m = sixMonthsStart; m <= monthStart; m = m.AddMonths(1))
            {
                var mNext = m.AddMonths(1);
                monthlyLabels.Add(m.ToString("MMM", System.Globalization.CultureInfo.InvariantCulture));
                monthlyTotals.Add(recentPayments
                    .Where(p => p.paymentDate >= m && p.paymentDate < mNext)
                    .Sum(p => p.amount));
            }

            decimal? revenueChangePercent = null;
            if (monthlyTotals.Count >= 2)
            {
                var previous = monthlyTotals[^2];
                var current = monthlyTotals[^1];
                if (previous > 0m)
                {
                    revenueChangePercent = Math.Round((current - previous) / previous * 100m, 0);
                }
            }

            // 8. Plan mix (active, unexpired memberships by plan)
            var planMixRows = await _context.Memberships
                .AsNoTracking()
                .Include(ms => ms.Plan)
                .Where(ms => ms.isActive &&
                             ms.endDate >= today &&
                             ms.Member != null &&
                             ms.Member.isActive)
                .ToListAsync();

            var planMixTotal = planMixRows.Count;
            var planMix = planMixRows
                .GroupBy(ms => ms.Plan != null ? ms.Plan.planName : "Unknown Plan")
                .Select(g => new DashboardPlanMixItemViewModel
                {
                    PlanName = g.Key,
                    Count = g.Count(),
                    Percent = planMixTotal == 0 ? 0 : (int)Math.Round(g.Count() * 100.0 / planMixTotal)
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.PlanName)
                .ToList();

            // 9. Expiring soon list (within 7 days + recently expired)
            var expiringRows = await _context.Memberships
                .AsNoTracking()
                .Include(ms => ms.Member)
                .Include(ms => ms.Plan)
                .Where(ms => ms.isActive &&
                             ms.Member != null &&
                             ms.Member.isActive &&
                             ms.endDate >= recentlyExpiredFrom &&
                             ms.endDate <= sevenDaysOut)
                .OrderBy(ms => ms.endDate)
                .ThenBy(ms => ms.Member!.fullName)
                .Take(8)
                .ToListAsync();

            var expiringSoon = expiringRows.Select(ms =>
            {
                var days = (ms.endDate.Date - today).Days;
                return new DashboardExpiringItemViewModel
                {
                    MemberId = ms.memberId,
                    FullName = ms.Member!.fullName,
                    ContactNumber = ms.Member.contactNumber ?? string.Empty,
                    PlanName = ms.Plan?.planName ?? "Unknown Plan",
                    EndDate = ms.endDate.Date,
                    DaysRemaining = Math.Max(0, days),
                    Status = days < 0 ? "Expired" : "Expiring"
                };
            }).ToList();

            // 10. Latest payments (non-voided)
            var latestPaymentRows = await _context.Payments
                .AsNoTracking()
                .Where(p => !p.isVoided)
                .OrderByDescending(p => p.paymentDate)
                .ThenByDescending(p => p.paymentId)
                .Take(5)
                .ToListAsync();

            var memberIds = latestPaymentRows.Select(p => p.memberId).Distinct().ToList();
            var membershipIds = latestPaymentRows.Select(p => p.membershipId).Distinct().ToList();
            var staffIds = latestPaymentRows.Select(p => p.recordedByStaffId).Distinct().ToList();

            var memberNames = await _context.Members
                .AsNoTracking()
                .Where(m => memberIds.Contains(m.memberId))
                .ToDictionaryAsync(m => m.memberId, m => m.fullName);

            var planNames = await _context.Memberships
                .AsNoTracking()
                .Include(ms => ms.Plan)
                .Where(ms => membershipIds.Contains(ms.membershipId))
                .ToDictionaryAsync(
                    ms => ms.membershipId,
                    ms => ms.Plan != null ? ms.Plan.planName : "—");

            var staffNames = await _context.Staff
                .AsNoTracking()
                .Where(s => staffIds.Contains(s.StaffID))
                .ToDictionaryAsync(s => s.StaffID, s => s.fullName);

            var latestPayments = latestPaymentRows
                .Select(p => new DashboardPaymentItemViewModel
                {
                    PaymentId = p.paymentId,
                    PaymentDate = p.paymentDate,
                    MemberName = memberNames.TryGetValue(p.memberId, out var mn) ? mn : "—",
                    PlanName = planNames.TryGetValue(p.membershipId, out var pn) ? pn : "—",
                    PaymentType = p.paymentType,
                    RecordedByStaffName = staffNames.TryGetValue(p.recordedByStaffId, out var sn) ? sn : "—",
                    Amount = p.amount
                })
                .ToList();

            return new AdminDashboardViewModel
            {
                AsOfDate = today,
                ActiveMembersCount = activeMembersCount,
                ExpiringIn7DaysCount = expiringIn7DaysCount,
                RevenueThisMonth = revenueThisMonth,
                CheckInsTodayCount = checkInsTodayCount,
                OnFloorCount = onFloorCount,
                NewMembersThisMonthCount = newMembersThisMonthCount,
                DailyCheckInLabels = dailyLabels,
                DailyCheckInCounts = dailyCounts,
                MonthlyRevenueLabels = monthlyLabels,
                MonthlyRevenueTotals = monthlyTotals,
                RevenueChangePercent = revenueChangePercent,
                PlanMix = planMix,
                ExpiringSoon = expiringSoon,
                LatestPayments = latestPayments
            };
        }
    }
}
