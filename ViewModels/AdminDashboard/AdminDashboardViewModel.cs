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
        public int OnFloorCount { get; set; }
        public int NewMembersThisMonthCount { get; set; }

        // Check-ins last 7 days
        public List<string> DailyCheckInLabels { get; set; } = new();
        public List<int> DailyCheckInCounts { get; set; } = new();

        // Revenue last 6 months
        public List<string> MonthlyRevenueLabels { get; set; } = new();
        public List<decimal> MonthlyRevenueTotals { get; set; } = new();
        /// <summary>Month-over-month % change for the current vs previous month. Null if previous is zero.</summary>
        public decimal? RevenueChangePercent { get; set; }

        public List<DashboardPlanMixItemViewModel> PlanMix { get; set; } = new();

        public List<DashboardExpiringItemViewModel> ExpiringSoon { get; set; } = new();
        public List<DashboardPaymentItemViewModel> LatestPayments { get; set; } = new();
    }

    public class DashboardPlanMixItemViewModel
    {
        public string PlanName { get; set; } = string.Empty;
        public int Count { get; set; }
        public int Percent { get; set; }
    }

    public class DashboardExpiringItemViewModel
    {
        public int MemberId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public DateTime EndDate { get; set; }
        public int DaysRemaining { get; set; }
        public string Status { get; set; } = string.Empty; // Expiring | Expired
    }

    public class DashboardPaymentItemViewModel
    {
        public int PaymentId { get; set; }
        public DateTime PaymentDate { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public string RecordedByStaffName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
