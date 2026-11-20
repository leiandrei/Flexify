using System;
using System.Reflection;
using System.Threading.Tasks;
using GymCrudApp.Controllers;
using GymCrudApp.Data;
using GymCrudApp.Models;
using GymCrudApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GymCrudApp.Tests
{
    public class AdminDashboardServiceTests
    {
        private static GymDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<GymDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new GymDbContext(options);

            context.MembershipPlans.Add(new MembershipPlans
            {
                planId = 1,
                planName = "Monthly Basic",
                planPrice = 1500m,
                durationDays = 30,
                planDescription = "30-day access",
                isActive = true
            });

            // Member 1: Active subscription (expires in 20 days), joined last month
            context.Members.Add(new Members
            {
                memberId = 1,
                userId = 11,
                fullName = "Active Veteran",
                userEmail = "veteran@gmail.com",
                contactNumber = "09170000011",
                birthdate = new DateTime(1998, 1, 1),
                joinDate = new DateTime(2026, 10, 15),
                isActive = true
            });
            context.Memberships.Add(new Memberships
            {
                membershipId = 1,
                memberId = 1,
                planId = 1,
                startDate = new DateTime(2026, 11, 19),
                endDate = new DateTime(2026, 12, 19),
                isActive = true
            });

            // Member 2: Expiring in 5 days, joined this month (Nov 2026)
            context.Members.Add(new Members
            {
                memberId = 2,
                userId = 12,
                fullName = "Expiring Newbie",
                userEmail = "expiring@gmail.com",
                contactNumber = "09170000012",
                birthdate = new DateTime(2000, 3, 10),
                joinDate = new DateTime(2026, 11, 4),
                isActive = true
            });
            context.Memberships.Add(new Memberships
            {
                membershipId = 2,
                memberId = 2,
                planId = 1,
                startDate = new DateTime(2026, 11, 4),
                endDate = new DateTime(2026, 12, 4), // 5 days after Nov 29
                isActive = true
            });

            // Member 3: Expired 3 days ago, joined this month (Nov 2026)
            context.Members.Add(new Members
            {
                memberId = 3,
                userId = 13,
                fullName = "Expired Member",
                userEmail = "expired@gmail.com",
                contactNumber = "09170000013",
                birthdate = new DateTime(1997, 8, 20),
                joinDate = new DateTime(2026, 11, 1),
                isActive = true
            });
            context.Memberships.Add(new Memberships
            {
                membershipId = 3,
                memberId = 3,
                planId = 1,
                startDate = new DateTime(2026, 10, 27),
                endDate = new DateTime(2026, 11, 26),
                isActive = true
            });

            // Payments: 2 valid in Nov 2026, 1 voided in Nov 2026, 1 valid in Oct 2026
            context.Payments.AddRange(
                new Payments
                {
                    paymentId = 1,
                    memberId = 1,
                    membershipId = 1,
                    amount = 1500m,
                    paymentType = "GCash",
                    referenceNumber = "GCASH-01",
                    paymentDate = new DateTime(2026, 11, 19),
                    recordedByStaffId = 1,
                    isVoided = false
                },
                new Payments
                {
                    paymentId = 2,
                    memberId = 2,
                    membershipId = 2,
                    amount = 1500m,
                    paymentType = "Cash",
                    paymentDate = new DateTime(2026, 11, 4),
                    recordedByStaffId = 1,
                    isVoided = false
                },
                new Payments
                {
                    paymentId = 3,
                    memberId = 2,
                    membershipId = 2,
                    amount = 9999m,
                    paymentType = "Cash",
                    paymentDate = new DateTime(2026, 11, 5),
                    recordedByStaffId = 1,
                    isVoided = true // Must be excluded from revenue
                },
                new Payments
                {
                    paymentId = 4,
                    memberId = 1,
                    membershipId = 1,
                    amount = 1500m,
                    paymentType = "Maya",
                    referenceNumber = "MAYA-01",
                    paymentDate = new DateTime(2026, 10, 15), // Prior month
                    recordedByStaffId = 1,
                    isVoided = false
                }
            );

            // Attendance: 2 check-ins on Nov 29, 1 check-in on Nov 27
            context.Attendance.AddRange(
                new Attendance { attendanceId = 1, memberId = 1, checkIn = new DateTime(2026, 11, 29, 7, 30, 0) },
                new Attendance { attendanceId = 2, memberId = 2, checkIn = new DateTime(2026, 11, 29, 17, 0, 0) },
                new Attendance { attendanceId = 3, memberId = 1, checkIn = new DateTime(2026, 11, 27, 8, 0, 0) }
            );

            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task GetDashboardMetricsAsync_ComputesAllFiveKpiCardsAccurately()
        {
            using var context = CreateContext();
            var service = new AdminDashboardService(context);
            var targetDate = new DateTime(2026, 11, 29);

            var metrics = await service.GetDashboardMetricsAsync(targetDate);

            Assert.Equal(2, metrics.ActiveMembersCount);         // Member 1 + Member 2
            Assert.Equal(1, metrics.ExpiringIn7DaysCount);       // Member 2 only
            Assert.Equal(3000m, metrics.RevenueThisMonth);       // 1500 + 1500 (excludes 9999 voided & Oct payment)
            Assert.Equal(2, metrics.CheckInsTodayCount);         // 2 check-ins on Nov 29
            Assert.Equal(2, metrics.NewMembersThisMonthCount);   // Member 2 + Member 3 joined in Nov 2026
        }

        [Fact]
        public async Task GetDashboardMetricsAsync_PopulatesChartSeriesAccurately()
        {
            using var context = CreateContext();
            var service = new AdminDashboardService(context);
            var targetDate = new DateTime(2026, 11, 29);

            var metrics = await service.GetDashboardMetricsAsync(targetDate);

            Assert.Equal(7, metrics.DailyCheckInLabels.Count);
            Assert.Equal(7, metrics.DailyCheckInCounts.Count);
            Assert.Equal(2, metrics.DailyCheckInCounts[6]); // Nov 29
            Assert.Equal(1, metrics.DailyCheckInCounts[4]); // Nov 27

            Assert.Equal(6, metrics.MonthlyRevenueLabels.Count);
            Assert.Equal(6, metrics.MonthlyRevenueTotals.Count);
            Assert.Equal(3000m, metrics.MonthlyRevenueTotals[5]); // Nov 2026
            Assert.Equal(1500m, metrics.MonthlyRevenueTotals[4]); // Oct 2026
        }

        [Fact]
        public void AdminDashboardController_RequiresAdminRoleOnly()
        {
            var attr = typeof(AdminDashboardController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(attr);
            Assert.Equal("Admin", attr!.Roles);
        }
    }
}