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

            // 4. Check-Ins Today
            var checkInsTodayCount = await _context.Attendance
                .AsNoTracking()
                .Where(a => a.checkIn >= today && a.checkIn < tomorrow)
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
                dailyLabels.Add(d.ToString("MMM dd"));
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
                monthlyLabels.Add(m.ToString("MMM yyyy"));
                monthlyTotals.Add(recentPayments
                    .Where(p => p.paymentDate >= m && p.paymentDate < mNext)
                    .Sum(p => p.amount));
            }

            return new AdminDashboardViewModel
            {
                AsOfDate = today,
                ActiveMembersCount = activeMembersCount,
                ExpiringIn7DaysCount = expiringIn7DaysCount,
                RevenueThisMonth = revenueThisMonth,
                CheckInsTodayCount = checkInsTodayCount,
                NewMembersThisMonthCount = newMembersThisMonthCount,
                DailyCheckInLabels = dailyLabels,
                DailyCheckInCounts = dailyCounts,
                MonthlyRevenueLabels = monthlyLabels,
                MonthlyRevenueTotals = monthlyTotals
            };
        }
    }
}