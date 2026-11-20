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
    public class MemberPortalServiceTests
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
                planName = "Quarterly Pro",
                planPrice = 4000m,
                durationDays = 90,
                planDescription = "90-day access",
                isActive = true
            });

            // Member A (memberId = 1, userId = 10)
            context.Members.Add(new Members
            {
                memberId = 1,
                userId = 10,
                fullName = "Member A",
                userEmail = "membera@gmail.com",
                contactNumber = "09170000010",
                birthdate = new DateTime(2000, 1, 1),
                joinDate = new DateTime(2026, 11, 1),
                isActive = true
            });
            context.Memberships.Add(new Memberships
            {
                membershipId = 1,
                memberId = 1,
                planId = 1,
                startDate = new DateTime(2026, 11, 1),
                endDate = new DateTime(2026, 12, 10),
                isActive = true
            });
            context.Payments.Add(new Payments
            {
                paymentId = 1,
                memberId = 1,
                membershipId = 1,
                amount = 4000m,
                paymentType = "GCash",
                referenceNumber = "GCASH-A001",
                paymentDate = new DateTime(2026, 11, 1),
                recordedByStaffId = 1,
                isVoided = false
            });
            context.Attendance.Add(new Attendance
            {
                attendanceId = 1,
                memberId = 1,
                checkIn = new DateTime(2026, 11, 28, 8, 0, 0),
                checkOut = new DateTime(2026, 11, 28, 9, 30, 0)
            });

            // Member B (memberId = 2, userId = 20)
            context.Members.Add(new Members
            {
                memberId = 2,
                userId = 20,
                fullName = "Member B",
                userEmail = "memberb@gmail.com",
                contactNumber = "09170000020",
                birthdate = new DateTime(1999, 5, 5),
                joinDate = new DateTime(2026, 11, 1),
                isActive = true
            });
            context.Memberships.Add(new Memberships
            {
                membershipId = 2,
                memberId = 2,
                planId = 1,
                startDate = new DateTime(2026, 11, 1),
                endDate = new DateTime(2026, 12, 25),
                isActive = true
            });
            context.Payments.Add(new Payments
            {
                paymentId = 2,
                memberId = 2,
                membershipId = 2,
                amount = 4000m,
                paymentType = "Cash",
                paymentDate = new DateTime(2026, 11, 1),
                recordedByStaffId = 1,
                isVoided = false
            });
            context.Attendance.Add(new Attendance
            {
                attendanceId = 2,
                memberId = 2,
                checkIn = new DateTime(2026, 11, 28, 10, 0, 0),
                checkOut = null
            });

            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task GetPortalForUserAsync_ReturnsOnlySignedInMembersData()
        {
            using var context = CreateContext();
            var service = new MemberPortalService(context);
            var today = new DateTime(2026, 11, 28);

            var portal = await service.GetPortalForUserAsync(10, "membera@gmail.com", today);

            Assert.NotNull(portal);
            Assert.Equal(1, portal!.MemberId);
            Assert.Equal("Member A", portal.FullName);
            Assert.Equal("Quarterly Pro", portal.PlanName);
            Assert.Equal("Active", portal.MembershipStatus);
            Assert.Equal(12, portal.DaysRemaining);

            Assert.Single(portal.Payments);
            Assert.Equal("GCASH-A001", portal.Payments[0].ReferenceNumber);

            Assert.Single(portal.AttendanceLogs);
            Assert.Equal(1, portal.AttendanceLogs[0].AttendanceId);
        }

        [Fact]
        public async Task GetPortalForMemberIdAsync_MemberAOpensMemberBUrl_ReturnsUnauthorized()
        {
            using var context = CreateContext();
            var service = new MemberPortalService(context);

            var (authorized, data) = await service.GetPortalForMemberIdAsync(
                requestedMemberId: 2,
                authenticatedUserId: 10,
                authenticatedEmail: "membera@gmail.com");

            Assert.False(authorized);
            Assert.Null(data);
        }

        [Fact]
        public async Task GetPortalForMemberIdAsync_MemberOpensOwnUrl_ReturnsAuthorized()
        {
            using var context = CreateContext();
            var service = new MemberPortalService(context);
            var today = new DateTime(2026, 11, 28);

            var (authorized, data) = await service.GetPortalForMemberIdAsync(
                requestedMemberId: 2,
                authenticatedUserId: 20,
                authenticatedEmail: "memberb@gmail.com",
                asOfDate: today);

            Assert.True(authorized);
            Assert.NotNull(data);
            Assert.Equal("Member B", data!.FullName);
            Assert.Equal(27, data.DaysRemaining);
        }

        [Fact]
        public void MemberPortalController_RequiresMemberRoleOnly()
        {
            var attr = typeof(MemberPortalController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(attr);
            Assert.Equal("Member", attr!.Roles);
        }
    }
}