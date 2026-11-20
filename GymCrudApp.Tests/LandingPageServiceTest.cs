using System;
using System.Linq;
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
    public class LandingPageServiceTests
    {
        private static GymDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<GymDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new GymDbContext(options);
        }

        [Fact]
        public async Task GetLandingPageDataAsync_ReturnsOnlyActivePlansOrderedByPrice()
        {
            using var context = CreateContext();
            context.MembershipPlans.AddRange(
                new MembershipPlans
                {
                    planId = 1,
                    planName = "Annual Elite",
                    planPrice = 14000m,
                    durationDays = 365,
                    planDescription = "Full year access",
                    isActive = true
                },
                new MembershipPlans
                {
                    planId = 2,
                    planName = "Legacy Promo (Archived)",
                    planPrice = 500m,
                    durationDays = 30,
                    planDescription = "Discontinued plan",
                    isActive = false
                },
                new MembershipPlans
                {
                    planId = 3,
                    planName = "Monthly Basic",
                    planPrice = 1500m,
                    durationDays = 30,
                    planDescription = "30-day access",
                    isActive = true
                }
            );
            await context.SaveChangesAsync();

            var service = new LandingPageService(context);
            var vm = await service.GetLandingPageDataAsync();

            Assert.Equal(2, vm.Plans.Count);
            Assert.Equal("Monthly Basic", vm.Plans[0].PlanName);
            Assert.Equal(1500m, vm.Plans[0].PlanPrice);
            Assert.Equal("Annual Elite", vm.Plans[1].PlanName);
            Assert.DoesNotContain(vm.Plans, p => p.PlanName.Contains("Archived"));
            Assert.False(string.IsNullOrWhiteSpace(vm.FacilityName));
            Assert.False(string.IsNullOrWhiteSpace(vm.ContactEmail));
        }

        [Fact]
        public async Task GetLandingPageDataAsync_NoPlansInDatabase_ReturnsEmptyListSafely()
        {
            using var context = CreateContext();
            var service = new LandingPageService(context);

            var vm = await service.GetLandingPageDataAsync();

            Assert.NotNull(vm);
            Assert.Empty(vm.Plans);
        }

        [Fact]
        public void HomeController_Index_AllowsAnonymousAccess()
        {
            var indexMethod = typeof(HomeController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .FirstOrDefault(m => m.Name == "Index");

            Assert.NotNull(indexMethod);
            var allowAnon = indexMethod!.GetCustomAttribute<AllowAnonymousAttribute>();
            Assert.NotNull(allowAnon);
        }
    }
}