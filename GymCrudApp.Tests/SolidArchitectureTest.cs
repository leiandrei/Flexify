using System;
using System.Linq;
using System.Reflection;
using GymCrudApp.Controllers;
using GymCrudApp.Data;
using GymCrudApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace GymCrudApp.Tests
{
    public class SolidArchitectureTest
    {
        [Fact]
        public void AllDomainServices_ImplementDedicatedServiceInterface()
        {
            var serviceAssembly = typeof(RenewalService).Assembly;

            var concreteServices = serviceAssembly
                .GetTypes()
                .Where(t => t.IsClass
                            && !t.IsAbstract
                            && t.Namespace == "GymCrudApp.Services"
                            && t.Name.EndsWith("Service")
                            && !typeof(BackgroundService).IsAssignableFrom(t))
                .ToList();

            Assert.NotEmpty(concreteServices);

            foreach (var serviceType in concreteServices)
            {
                var matchingInterface = serviceType
                    .GetInterfaces()
                    .FirstOrDefault(i => i.Name == $"I{serviceType.Name}");

                Assert.True(
                    matchingInterface != null,
                    $"{serviceType.Name} must implement interface I{serviceType.Name} to satisfy DIP and ISP.");
            }
        }

        [Fact]
        public void DomainControllers_DoNotDependDirectlyOnGymDbContext()
        {
            var controllerAssembly = typeof(HomeController).Assembly;

            var domainControllers = controllerAssembly
                .GetTypes()
                .Where(t => typeof(Controller).IsAssignableFrom(t)
                            && !t.IsAbstract
                            && t != typeof(AccountController))
                .ToList();

            Assert.NotEmpty(domainControllers);

            foreach (var controller in domainControllers)
            {
                var constructors = controller.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                foreach (var ctor in constructors)
                {
                    var injectsDbContext = ctor.GetParameters()
                        .Any(p => p.ParameterType == typeof(GymDbContext));

                    Assert.False(
                        injectsDbContext,
                        $"{controller.Name} injects GymDbContext directly. Delegate data access to an injected I*Service interface.");
                }
            }
        }
    }
}