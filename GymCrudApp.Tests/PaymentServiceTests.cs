using System;
using System.Threading.Tasks;
using GymCrudApp.Data;
using GymCrudApp.Models;
using GymCrudApp.Services;
using GymCrudApp.ViewModels.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace GymCrudApp.Tests
{
    public class PaymentServiceTests
    {
        private static GymDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<GymDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var context = new GymDbContext(options);

            var staffUser = new Users { userId = 1, userEmail = "staff@gmail.com", passwordHash = "hash", role = "Staff", isActive = true };
            context.Users.Add(staffUser);
            context.Staff.Add(new Staff { StaffID = 1, userId = 1, fullName = "Ana Reyes", position = "Front Desk", dateOfBirth = new DateTime(1998, 5, 10) });
            context.Members.Add(new Members
            {
                memberId = 1,
                userId = 2,
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
                planName = "Monthly Basic",
                planPrice = 1500m,
                durationDays = 30,
                planDescription = "30-day standard gym access",
                isActive = true
            });
            context.SaveChanges();

            return context;
        }

        [Fact]
        public async Task CreateAsync_ValidCashPayment_CreatesMembershipAndPaymentTogether()
        {
            using var context = CreateContext();
            var service = new PaymentService(context);

            var (success, error) = await service.CreateAsync(new PaymentCreateViewModel
            {
                MemberId = 1,
                PlanId = 1,
                StartDate = new DateTime(2026, 11, 24),
                Amount = 1500m,
                PaymentType = "Cash",
                PaymentDate = new DateTime(2026, 11, 24),
                RecordedByStaffId = 1
            });

            Assert.True(success);
            Assert.Null(error);
            Assert.Equal(1, await context.Memberships.CountAsync());
            Assert.Equal(1, await context.Payments.CountAsync());

            var membership = await context.Memberships.FirstAsync();
            var payment = await context.Payments.FirstAsync();
            Assert.Equal(new DateTime(2026, 12, 24), membership.endDate);
            Assert.Equal(membership.membershipId, payment.membershipId);
            Assert.Equal(1500m, payment.amount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public async Task CreateAsync_ZeroOrNegativeAmount_FailsWithoutCreatingMembership(decimal invalidAmount)
        {
            using var context = CreateContext();
            var service = new PaymentService(context);

            var (success, error) = await service.CreateAsync(new PaymentCreateViewModel
            {
                MemberId = 1,
                PlanId = 1,
                StartDate = new DateTime(2026, 11, 24),
                Amount = invalidAmount,
                PaymentType = "Cash",
                PaymentDate = new DateTime(2026, 11, 24),
                RecordedByStaffId = 1
            });

            Assert.False(success);
            Assert.NotNull(error);
            Assert.Equal(0, await context.Memberships.CountAsync());
            Assert.Equal(0, await context.Payments.CountAsync());
        }

        [Fact]
        public async Task CreateAsync_NonCashWithoutReference_FailsValidation()
        {
            using var context = CreateContext();
            var service = new PaymentService(context);

            var (success, error) = await service.CreateAsync(new PaymentCreateViewModel
            {
                MemberId = 1,
                PlanId = 1,
                StartDate = new DateTime(2026, 11, 24),
                Amount = 1500m,
                PaymentType = "GCash",
                ReferenceNumber = null,
                PaymentDate = new DateTime(2026, 11, 24),
                RecordedByStaffId = 1
            });

            Assert.False(success);
            Assert.Contains("Reference number", error);
        }

        [Fact]
        public async Task VoidAsync_ValidPayment_MarksAsVoidedAndExcludesFromTotal()
        {
            using var context = CreateContext();
            var service = new PaymentService(context);

            await service.CreateAsync(new PaymentCreateViewModel
            {
                MemberId = 1,
                PlanId = 1,
                StartDate = new DateTime(2026, 11, 24),
                Amount = 1500m,
                PaymentType = "GCash",
                ReferenceNumber = "GCASH-998877",
                PaymentDate = new DateTime(2026, 11, 24),
                RecordedByStaffId = 1
            });

            var payment = await context.Payments.FirstAsync();
            var (voidSuccess, voidError) = await service.VoidAsync(payment.paymentId);

            Assert.True(voidSuccess);
            Assert.Null(voidError);

            var updated = await context.Payments.FirstAsync();
            Assert.True(updated.isVoided);

            var indexVm = await service.GetFilteredAsync(null, null);
            Assert.Equal(0m, indexVm.TotalValidAmount);
        }

        [Fact]
        public async Task VoidAsync_AlreadyVoidedPayment_ReturnsError()
        {
            using var context = CreateContext();
            var service = new PaymentService(context);

            await service.CreateAsync(new PaymentCreateViewModel
            {
                MemberId = 1,
                PlanId = 1,
                StartDate = new DateTime(2026, 11, 24),
                Amount = 1500m,
                PaymentType = "Cash",
                PaymentDate = new DateTime(2026, 11, 24),
                RecordedByStaffId = 1
            });

            var payment = await context.Payments.FirstAsync();
            await service.VoidAsync(payment.paymentId);

            var (secondVoidSuccess, error) = await service.VoidAsync(payment.paymentId);

            Assert.False(secondVoidSuccess);
            Assert.Contains("already been voided", error);
        }
    }
}