using System;
using System.Threading.Tasks;
using GymCrudApp.Data;
using GymCrudApp.Models;
using GymCrudApp.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GymCrudApp.Tests
{
    public class Tier1EdgeCaseTests
    {
        private static GymDbContext CreateEmptyContext()
        {
            var options = new DbContextOptionsBuilder<GymDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new GymDbContext(options);
        }

        [Fact]
        public async Task MemberPortalService_MemberWithNoMembership_ReturnsSafeDefaults()
        {
            using var context = CreateEmptyContext();
            context.Members.Add(new Members
            {
                memberId = 1,
                userId = 50,
                fullName = "Sofia Garcia",
                userEmail = "sofia.garcia@gmail.com",
                contactNumber = "09204445566",
                birthdate = new DateTime(2002, 1, 30),
                joinDate = new DateTime(2026, 11, 20),
                isActive = true
            });
            await context.SaveChangesAsync();

            var service = new MemberPortalService(context);
            var portal = await service.GetPortalForUserAsync(50, "sofia.garcia@gmail.com", new DateTime(2026, 11, 30));

            Assert.NotNull(portal);
            Assert.Equal("No Membership", portal!.MembershipStatus);
            Assert.Equal(0, portal.DaysRemaining);
            Assert.Null(portal.PlanName);
            Assert.Empty(portal.Payments);
            Assert.Empty(portal.AttendanceLogs);
            Assert.Empty(portal.AvailablePlans);
            Assert.Equal(0, portal.VisitsThisMonth);
        }

        [Fact]
        public async Task AttendanceService_DeactivatedMemberWithValidPlan_BlocksCheckIn()
        {
            using var context = CreateEmptyContext();
            context.MembershipPlans.Add(new MembershipPlans
            {
                planId = 1,
                planName = "Monthly Basic",
                planPrice = 1500m,
                durationDays = 30,
                planDescription = "30-day access",
                isActive = true
            });
            context.Members.Add(new Members
            {
                memberId = 1,
                userId = 60,
                fullName = "Deactivated Member",
                userEmail = "inactive@gmail.com",
                contactNumber = "09170000060",
                birthdate = new DateTime(1999, 4, 15),
                joinDate = new DateTime(2026, 11, 1),
                isActive = false
            });
            context.Memberships.Add(new Memberships
            {
                membershipId = 1,
                memberId = 1,
                planId = 1,
                startDate = new DateTime(2026, 11, 1),
                endDate = new DateTime(2026, 12, 15),
                isActive = true
            });
            await context.SaveChangesAsync();

            var service = new AttendanceService(context);
            var (success, warning, error) = await service.CheckInAsync(1, new DateTime(2026, 11, 30, 9, 0, 0));

            Assert.False(success);
            Assert.Null(warning);
            Assert.Contains("inactive", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AdminDashboardService_EmptyDatabase_ReturnsZeroMetricsWithoutError()
        {
            using var context = CreateEmptyContext();
            var service = new AdminDashboardService(context);

            var metrics = await service.GetDashboardMetricsAsync(new DateTime(2026, 11, 30));

            Assert.Equal(0, metrics.ActiveMembersCount);
            Assert.Equal(0, metrics.ExpiringIn7DaysCount);
            Assert.Equal(0m, metrics.RevenueThisMonth);
            Assert.Equal(0, metrics.CheckInsTodayCount);
            Assert.Equal(0, metrics.OnFloorCount);
            Assert.Equal(0, metrics.NewMembersThisMonthCount);
            Assert.Equal(7, metrics.DailyCheckInCounts.Count);
            Assert.Equal(6, metrics.MonthlyRevenueTotals.Count);
            Assert.Empty(metrics.ExpiringSoon);
            Assert.Empty(metrics.LatestPayments);
            Assert.Empty(metrics.PlanMix);
            Assert.Null(metrics.RevenueChangePercent);
        }
    }
}