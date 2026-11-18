using System;
using System.Threading.Tasks;
using GymCrudApp.Data;
using GymCrudApp.Models;
using GymCrudApp.Services;
using GymCrudApp.ViewModels.Memberships;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GymCrudApp.Tests
{
    public class MembershipServiceTests
    {
        private static GymDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<GymDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new GymDbContext(options);
        }

        [Fact]
        public void CalculateEndDate_AddsDurationDaysToStartDate()
        {
            using var context = CreateInMemoryContext();
            var service = new MembershipService(context);
            var startDate = new DateTime(2026, 11, 23);

            var endDate = service.CalculateEndDate(startDate, 30);

            Assert.Equal(new DateTime(2026, 12, 23), endDate);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void CalculateEndDate_InvalidDuration_ThrowsArgumentOutOfRangeException(int invalidDays)
        {
            using var context = CreateInMemoryContext();
            var service = new MembershipService(context);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.CalculateEndDate(new DateTime(2026, 11, 23), invalidDays));
        }

        [Fact]
        public void ComputeStatus_MoreThanSevenDaysRemaining_ReturnsActive()
        {
            using var context = CreateInMemoryContext();
            var service = new MembershipService(context);
            var asOfDate = new DateTime(2026, 11, 23);
            var endDate = asOfDate.AddDays(15);

            var daysRemaining = service.CalculateDaysRemaining(endDate, asOfDate);
            var status = service.ComputeStatus(endDate, isActive: true, asOfDate: asOfDate);

            Assert.Equal(15, daysRemaining);
            Assert.Equal("Active", status);
        }

        [Theory]
        [InlineData(7)]
        [InlineData(3)]
        [InlineData(0)]
        public void ComputeStatus_ZeroToSevenDaysRemaining_ReturnsExpiring(int daysUntilExpiry)
        {
            using var context = CreateInMemoryContext();
            var service = new MembershipService(context);
            var asOfDate = new DateTime(2026, 11, 23);
            var endDate = asOfDate.AddDays(daysUntilExpiry);

            var daysRemaining = service.CalculateDaysRemaining(endDate, asOfDate);
            var status = service.ComputeStatus(endDate, isActive: true, asOfDate: asOfDate);

            Assert.Equal(daysUntilExpiry, daysRemaining);
            Assert.Equal("Expiring", status);
        }

        [Fact]
        public void ComputeStatus_PastEndDate_ReturnsExpiredAndZeroDaysRemaining()
        {
            using var context = CreateInMemoryContext();
            var service = new MembershipService(context);
            var asOfDate = new DateTime(2026, 11, 23);
            var endDate = asOfDate.AddDays(-2);

            var daysRemaining = service.CalculateDaysRemaining(endDate, asOfDate);
            var status = service.ComputeStatus(endDate, isActive: true, asOfDate: asOfDate);

            Assert.Equal(0, daysRemaining);
            Assert.Equal("Expired", status);
        }

        [Fact]
        public void ComputeStatus_WhenDeactivated_ReturnsInactive()
        {
            using var context = CreateInMemoryContext();
            var service = new MembershipService(context);
            var asOfDate = new DateTime(2026, 11, 23);
            var endDate = asOfDate.AddDays(20);

            var status = service.ComputeStatus(endDate, isActive: false, asOfDate: asOfDate);

            Assert.Equal("Inactive", status);
        }

        [Fact]
        public async Task CreateAsync_ValidMemberAndPlan_ComputesEndDateAndPersistsMembership()
        {
            using var context = CreateInMemoryContext();
            context.Members.Add(new Members
            {
                memberId = 1,
                userId = 10,
                fullName = "Juan Dela Cruz",
                userEmail = "juan@gmail.com",
                contactNumber = "09171234567",
                birthdate = new DateTime(2000, 1, 1),
                joinDate = new DateTime(2026, 11, 1),
                isActive = true
            });
            context.MembershipPlans.Add(new MembershipPlans
            {
                planId = 1,
                planName = "Quarterly Pro",
                planPrice = 3500m,
                durationDays = 90,
                planDescription = "90-day full gym access",
                isActive = true
            });
            await context.SaveChangesAsync();

            var service = new MembershipService(context);
            var startDate = new DateTime(2026, 11, 23);

            var (success, errorMessage) = await service.CreateAsync(new MembershipCreateViewModel
            {
                MemberId = 1,
                PlanId = 1,
                StartDate = startDate
            });

            var saved = await context.Memberships.FirstOrDefaultAsync();
            Assert.True(success);
            Assert.Null(errorMessage);
            Assert.NotNull(saved);
            Assert.Equal(new DateTime(2027, 2, 21), saved!.endDate);
        }
    }
}