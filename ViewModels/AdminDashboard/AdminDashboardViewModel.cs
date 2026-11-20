using System;
using System.Collections.Generic;

namespace GymCrudApp.ViewModels.AdminDashboard
{
    public class AdminDashboardViewModel
    {
        public DateTime AsOfDate { get; set; }

        // 5 Core KPI Cards
        public int ActiveMembersCount { get; set; }
        public int ExpiringIn7DaysCount { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public int CheckInsTodayCount { get; set; }
        public int NewMembersThisMonthCount { get; set; }

        // Optional Chart.js Series (Last 7 Days Check-Ins & Last 6 Months Revenue)
        public List<string> DailyCheckInLabels { get; set; } = new();
        public List<int> DailyCheckInCounts { get; set; } = new();

        public List<string> MonthlyRevenueLabels { get; set; } = new();
        public List<decimal> MonthlyRevenueTotals { get; set; } = new();
    }
}