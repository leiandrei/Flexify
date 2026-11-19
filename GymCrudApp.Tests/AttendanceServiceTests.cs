using System;
using System.Threading.Tasks;
using GymCrudApp.Data;
using GymCrudApp.Models;
using GymCrudApp.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GymCrudApp.Tests
{
    public class AttendanceServiceTests
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

            // Member 1: Active membership
            context.Members.Add(new Members
            {
                memberId = 1,
                userId = 101,
                fullName = "Active Member",
                userEmail = "active@gmail.com",
                contactNumber = "09170000001",
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
                endDate = new DateTime(2026, 12, 15),
                isActive = true
            });

            // Member 2: Expired membership
            context.Members.Add(new Members
            {
                memberId = 2,
                userId = 102,
                fullName = "Expired Member",
                userEmail = "expired@gmail.com",
                contactNumber = "09170000002",
                birthdate = new DateTime(2000, 1, 1),
                joinDate = new DateTime(2026, 10, 1),
                isActive = true
            });
            context.Memberships.Add(new Memberships
            {
                membershipId = 2,
                memberId = 2,
                planId = 1,
                startDate = new DateTime(2026, 10, 1),
                endDate = new DateTime(2026, 11, 1),
                isActive = true
            });

            // Member 3: Expiring within 7 days
            context.Members.Add(new Members
            {
                memberId = 3,
                userId = 103,
                fullName = "Expiring Member",
                userEmail = "expiring@gmail.com",
                contactNumber = "09170000003",
                birthdate = new DateTime(2000, 1, 1),
                joinDate = new DateTime(2026, 11, 1),
                isActive = true
            });
            context.Memberships.Add(new Memberships
            {
                membershipId = 3,
                memberId = 3,
                planId = 1,
                startDate = new DateTime(2026, 11, 1),
                endDate = new DateTime(2026, 11, 28),
                isActive = true
            });

            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task CheckInAsync_ActiveMembership_CreatesOpenAttendanceRecord()
        {
            using var context = CreateContext();
            var service = new AttendanceService(context);
            var checkInTime = new DateTime(2026, 11, 25, 9, 0, 0);

            var (success, warning, error) = await service.CheckInAsync(1, checkInTime);

            Assert.True(success);
            Assert.Null(warning);
            Assert.Null(error);

            var record = await context.Attendance.FirstOrDefaultAsync();
            Assert.NotNull(record);
            Assert.Equal(checkInTime, record!.checkIn);
            Assert.Null(record.checkOut);
        }

        [Fact]
        public async Task CheckInAsync_ExpiringMembership_SucceedsWithWarningMessage()
        {
            using var context = CreateContext();
            var service = new AttendanceService(context);
            var checkInTime = new DateTime(2026, 11, 25, 9, 0, 0);

            var (success, warning, error) = await service.CheckInAsync(3, checkInTime);

            Assert.True(success);
            Assert.NotNull(warning);
            Assert.Contains("expires in 3 day(s)", warning);
            Assert.Null(error);
        }

        [Fact]
        public async Task CheckInAsync_ExpiredMembership_BlocksCheckIn()
        {
            using var context = CreateContext();
            var service = new AttendanceService(context);
            var checkInTime = new DateTime(2026, 11, 25, 9, 0, 0);

            var (success, warning, error) = await service.CheckInAsync(2, checkInTime);

            Assert.False(success);
            Assert.Null(warning);
            Assert.Contains("expired", error, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, await context.Attendance.CountAsync());
        }

        [Fact]
        public async Task CheckInAsync_DuplicateOpenCheckInSameDay_BlocksSecondCheckIn()
        {
            using var context = CreateContext();
            var service = new AttendanceService(context);
            var morning = new DateTime(2026, 11, 25, 8, 30, 0);
            var afternoon = new DateTime(2026, 11, 25, 14, 0, 0);

            var first = await service.CheckInAsync(1, morning);
            var second = await service.CheckInAsync(1, afternoon);

            Assert.True(first.Success);
            Assert.False(second.Success);
            Assert.Contains("open check-in today", second.ErrorMessage);
            Assert.Equal(1, await context.Attendance.CountAsync());
        }

        [Fact]
        public async Task CheckInAsync_AfterCheckOutSameDay_AllowsSecondSession()
        {
            using var context = CreateContext();
            var service = new AttendanceService(context);
            var morningIn = new DateTime(2026, 11, 25, 8, 0, 0);
            var morningOut = new DateTime(2026, 11, 25, 9, 30, 0);
            var eveningIn = new DateTime(2026, 11, 25, 18, 0, 0);

            await service.CheckInAsync(1, morningIn);
            var openRecord = await context.Attendance.FirstAsync();
            var checkOutResult = await service.CheckOutAsync(openRecord.attendanceId, morningOut);
            var eveningResult = await service.CheckInAsync(1, eveningIn);

            Assert.True(checkOutResult.Success);
            Assert.True(eveningResult.Success);
            Assert.Equal(2, await context.Attendance.CountAsync());
        }
    }
}