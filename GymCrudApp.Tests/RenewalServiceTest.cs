using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GymCrudApp.Controllers;
using GymCrudApp.Data;
using GymCrudApp.Models;
using GymCrudApp.Services;
using GymCrudApp.ViewModels.Renewals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace GymCrudApp.Tests
{
    public class RenewalServiceTests
    {
        private static GymDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<GymDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
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

            // Member 1: Expiring in 4 days (endDate = Dec 6, 2026)
            context.Members.Add(new Members
            {
                memberId = 1,
                userId = 11,
                fullName = "Bianca Lim",
                userEmail = "bianca.lim@gmail.com",
                contactNumber = "09170000011",
                birthdate = new DateTime(2001, 2, 2),
                joinDate = new DateTime(2026, 11, 6),
                isActive = true
            });
            context.Memberships.Add(new Memberships
            {
                membershipId = 1,
                memberId = 1,
                planId = 1,
                startDate = new DateTime(2026, 11, 6),
                endDate = new DateTime(2026, 12, 6),
                isActive = true
            });

            // Member 2: Already expired 7 days ago (endDate = Nov 25, 2026)
            context.Members.Add(new Members
            {
                memberId = 2,
                userId = 12,
                fullName = "Rafael Torres",
                userEmail = "rafael.torres@gmail.com",
                contactNumber = "09170000012",
                birthdate = new DateTime(1998, 6, 12),
                joinDate = new DateTime(2026, 10, 26),
                isActive = true
            });
            context.Memberships.Add(new Memberships
            {
                membershipId = 2,
                memberId = 2,
                planId = 1,
                startDate = new DateTime(2026, 10, 26),
                endDate = new DateTime(2026, 11, 25),
                isActive = true
            });

            // Member 3: Active with 25 days remaining (should NOT appear in 7-day expiring list)
            context.Members.Add(new Members
            {
                memberId = 3,
                userId = 13,
                fullName = "Carlos Mendoza",
                userEmail = "carlos.mendoza@gmail.com",
                contactNumber = "09170000013",
                birthdate = new DateTime(1999, 9, 9),
                joinDate = new DateTime(2026, 11, 27),
                isActive = true
            });
            context.Memberships.Add(new Memberships
            {
                membershipId = 3,
                memberId = 3,
                planId = 1,
                startDate = new DateTime(2026, 11, 27),
                endDate = new DateTime(2026, 12, 27),
                isActive = true
            });

            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task RenewMembershipAsync_UnexpiredSubscription_StartsDayAfterCurrentEndDate()
        {
            using var context = CreateContext();
            var service = new RenewalService(context);
            var today = new DateTime(2026, 12, 2);

            var result = await service.RenewMembershipAsync(
                new OneClickRenewalViewModel
                {
                    MemberId = 1, // Ends Dec 6, 2026
                    PlanId = 1,   // 30 days
                    Amount = 1500m,
                    PaymentType = "GCash",
                    ReferenceNumber = "GCASH-REN-01"
                },
                recordedByStaffId: 2,
                asOfDate: today);

            Assert.True(result.Succeeded);
            Assert.Equal(new DateTime(2026, 12, 7), result.NewStartDate);
            Assert.Equal(new DateTime(2027, 1, 6), result.NewEndDate);

            // Prior membership deactivated; new membership + payment saved atomically
            var oldMembership = await context.Memberships.FindAsync(1);
            Assert.False(oldMembership!.isActive);
            Assert.Equal(1, await context.Payments.CountAsync(p => p.memberId == 1 && !p.isVoided));
        }

        [Fact]
        public async Task RenewMembershipAsync_ExpiredSubscription_StartsToday()
        {
            using var context = CreateContext();
            var service = new RenewalService(context);
            var today = new DateTime(2026, 12, 2);

            var result = await service.RenewMembershipAsync(
                new OneClickRenewalViewModel
                {
                    MemberId = 2, // Expired Nov 25, 2026
                    PlanId = 1,   // 30 days
                    Amount = 1500m,
                    PaymentType = "Cash"
                },
                recordedByStaffId: 2,
                asOfDate: today);

            Assert.True(result.Succeeded);
            Assert.Equal(new DateTime(2026, 12, 2), result.NewStartDate);
            Assert.Equal(new DateTime(2027, 1, 1), result.NewEndDate);
        }

        [Fact]
        public async Task GetExpiringSoonMembersAsync_FiltersToExpiringAndExpiredOnly()
        {
            using var context = CreateContext();
            var service = new RenewalService(context);
            var today = new DateTime(2026, 12, 2);

            var queue = await service.GetExpiringSoonMembersAsync(withinDays: 7, includeRecentlyExpired: true, asOfDate: today);

            Assert.Equal(2, queue.Count);
            Assert.Contains(queue, x => x.FullName == "Rafael Torres" && x.Status == "Expired");
            Assert.Contains(queue, x => x.FullName == "Bianca Lim" && x.Status == "Expiring" && x.DaysRemaining == 4);
            Assert.DoesNotContain(queue, x => x.FullName == "Carlos Mendoza");
        }

        [Fact]
        public async Task DeactivateExpiredMembershipsAsync_FlipsOnlyPastDueSubscriptions()
        {
            using var context = CreateContext();
            var service = new RenewalService(context);
            var today = new DateTime(2026, 12, 2);

            var count = await service.DeactivateExpiredMembershipsAsync(today);

            Assert.Equal(1, count);
            Assert.False((await context.Memberships.FindAsync(2))!.isActive); // Expired Nov 25
            Assert.True((await context.Memberships.FindAsync(1))!.isActive);  // Ends Dec 6
            Assert.True((await context.Memberships.FindAsync(3))!.isActive);  // Ends Dec 27
        }

        [Fact]
        public void RenewalsController_RequiresAdminOrStaffRole()
        {
            var attr = typeof(RenewalsController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(attr);
            Assert.Equal("Admin,Staff", attr!.Roles);
        }
    }
}