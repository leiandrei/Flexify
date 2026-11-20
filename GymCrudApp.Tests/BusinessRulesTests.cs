using System;
using System.Threading.Tasks;
using GymCrudApp.Data;
using GymCrudApp.Models;
using GymCrudApp.Services;
using GymCrudApp.ViewModels.Renewals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace GymCrudApp.Tests
{
    public class BusinessRulesTests
    {
        private static GymDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<GymDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var context = new GymDbContext(options);

            context.Staff.Add(new Staff
            {
                StaffID = 1,
                userId = 2,
                fullName = "Front Desk Staff",
                position = "Receptionist"
            });

            context.MembershipPlans.Add(new MembershipPlans
            {
                planId = 1,
                planName = "Monthly Standard",
                planPrice = 1500m,
                durationDays = 30,
                planDescription = "30-day access",
                isActive = true
            });

            context.Members.Add(new Members
            {
                memberId = 1,
                userId = 10,
                fullName = "Carlos Mendoza",
                userEmail = "carlos@gmail.com",
                contactNumber = "09173334444",
                birthdate = new DateTime(1999, 5, 10),
                joinDate = new DateTime(2026, 11, 1),
                isActive = true
            });

            context.Memberships.Add(new Memberships
            {
                membershipId = 1,
                memberId = 1,
                planId = 1,
                startDate = new DateTime(2026, 11, 15),
                endDate = new DateTime(2026, 12, 15),
                isActive = true
            });

            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task RenewalRule_NonCashPaymentWithoutReference_RejectedAtomically()
        {
            using var context = CreateContext();
            var service = new RenewalService(context);

            var result = await service.RenewMembershipAsync(
                new OneClickRenewalViewModel
                {
                    MemberId = 1,
                    PlanId = 1,
                    Amount = 1500m,
                    PaymentType = "GCash",
                    ReferenceNumber = "   "
                },
                recordedByStaffId: 1,
                asOfDate: new DateTime(2026, 12, 3));

            Assert.False(result.Succeeded);
            Assert.Contains("Reference number", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(1, await context.Memberships.CountAsync());
            Assert.Equal(0, await context.Payments.CountAsync());
        }

        [Fact]
        public async Task AttendanceRule_DuplicateOpenCheckInSameDay_IsBlocked()
        {
            using var context = CreateContext();
            var service = new AttendanceService(context);
            var morning = new DateTime(2026, 12, 3, 8, 0, 0);
            var afternoon = new DateTime(2026, 12, 3, 16, 0, 0);

            var first = await service.CheckInAsync(1, morning);
            var second = await service.CheckInAsync(1, afternoon);

            Assert.True(first.Success);
            Assert.False(second.Success);
            Assert.Contains("Check-in blocked", second.ErrorMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("already", second.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task StaffLookup_ReturnsLinkedStaffIdForAuthenticatedUser()
        {
            using var context = CreateContext();
            var service = new RenewalService(context);

            var linkedStaffId = await service.GetStaffIdByUserIdAsync(2);
            var unlinkedStaffId = await service.GetStaffIdByUserIdAsync(999);

            Assert.Equal(1, linkedStaffId);
            Assert.Null(unlinkedStaffId);
        }
    }
}