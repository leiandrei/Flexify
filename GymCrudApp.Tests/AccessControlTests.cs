using System.Linq;
using System.Reflection;
using GymCrudApp.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace GymCrudApp.Tests
{
    public class AccessControlTests
    {
        [Fact]
        public void StaffController_RequiresAdminRoleOnly()
        {
            var attr = typeof(StaffController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(attr);
            Assert.Equal("Admin", attr!.Roles);
        }

        [Fact]
        public void MembershipPlansController_AllowsStaffRead_RestrictsWriteToAdmin()
        {
            var classAttr = typeof(MembershipPlansController).GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(classAttr);
            Assert.Equal("Admin,Staff", classAttr!.Roles);

            string[] adminOnlyActions = { "Create", "Edit", "Delete", "DeleteConfirmed" };
            var writeMethods = typeof(MembershipPlansController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => adminOnlyActions.Contains(m.Name))
                .ToList();

            Assert.NotEmpty(writeMethods);
            foreach (var method in writeMethods)
            {
                var methodAttr = method.GetCustomAttribute<AuthorizeAttribute>();
                Assert.NotNull(methodAttr);
                Assert.Equal("Admin", methodAttr!.Roles);
            }
        }

        [Fact]
        public void AttendanceController_DeleteActions_RequireAdminRoleOnly()
        {
            var classAttr = typeof(AttendanceController).GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(classAttr);
            Assert.Equal("Admin,Staff", classAttr!.Roles);

            var deleteMethods = typeof(AttendanceController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.Name == "Delete" || m.Name == "DeleteConfirmed")
                .ToList();

            Assert.Equal(2, deleteMethods.Count);
            foreach (var method in deleteMethods)
            {
                var methodAttr = method.GetCustomAttribute<AuthorizeAttribute>();
                Assert.NotNull(methodAttr);
                Assert.Equal("Admin", methodAttr!.Roles);
            }
        }

        [Theory]
        [InlineData(typeof(MembershipsController), "Admin,Staff")]
        [InlineData(typeof(PaymentsController), "Admin,Staff")]
        public void OperationalControllers_RequireAdminOrStaffRole(System.Type controllerType, string expectedRoles)
        {
            var attr = controllerType.GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(attr);
            Assert.Equal(expectedRoles, attr!.Roles);
        }

        [Fact]
        public void AllNonPublicControllers_HaveAuthorizeAttribute()
        {
            var protectedControllers = typeof(StaffController).Assembly
                .GetTypes()
                .Where(t => typeof(Controller).IsAssignableFrom(t)
                            && !t.IsAbstract
                            && t != typeof(HomeController)
                            && t != typeof(AccountController));

            foreach (var controller in protectedControllers)
            {
                var hasAuthorize = controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any();
                Assert.True(hasAuthorize, $"{controller.Name} is missing an [Authorize] attribute.");
            }
        }

        [Theory]
        [InlineData("Login")]
        [InlineData("Register")]
        [InlineData("AccessDenied")]
        public void AccountController_PublicEndpoints_AllowAnonymous(string actionName)
        {
            var methods = typeof(AccountController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.Name == actionName)
                .ToList();

            Assert.NotEmpty(methods);
            foreach (var method in methods)
            {
                var allowAnon = method.GetCustomAttribute<AllowAnonymousAttribute>();
                Assert.NotNull(allowAnon);
            }
        }
    }
}