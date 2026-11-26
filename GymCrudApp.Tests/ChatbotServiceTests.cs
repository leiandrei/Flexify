using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
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
    public class ChatbotServiceTests
    {
        private class StubLlmChatService : ILlmChatService
        {
            private readonly string? _cannedReply;
            public string? LastSystemPrompt { get; private set; }

            public StubLlmChatService(string? cannedReply) => _cannedReply = cannedReply;

            public Task<string?> GenerateReplyAsync(
                string systemPrompt,
                string userMessage,
                CancellationToken cancellationToken = default)
            {
                LastSystemPrompt = systemPrompt;
                return Task.FromResult(_cannedReply);
            }
        }

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

            context.Members.Add(new Members
            {
                memberId = 1,
                userId = 10,
                fullName = "Andrea Reyes",
                userEmail = "andrea@gmail.com",
                contactNumber = "09171234567",
                birthdate = new DateTime(2000, 4, 15),
                joinDate = new DateTime(2026, 10, 1),
                isActive = true
            });

            context.Memberships.Add(new Memberships
            {
                membershipId = 1,
                memberId = 1,
                planId = 1,
                startDate = new DateTime(2026, 11, 1),
                endDate = new DateTime(2027, 1, 30),
                isActive = true
            });

            context.SaveChanges();
            return context;
        }

        private static ClaimsPrincipal CreateMemberPrincipal(int userId, string email, string name)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Email, email),
                new(ClaimTypes.Name, name),
                new(ClaimTypes.Role, "Member")
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        [Fact]
        public async Task ProcessMessageAsync_AuthenticatedMember_InjectsClaimsScopedContextIntoPrompt()
        {
            using var context = CreateContext();
            var stubLlm = new StubLlmChatService("Your Quarterly Pro plan expires on Jan 30, 2027.");
            var service = new ChatbotService(new MemberPortalService(context), new LandingPageService(context), stubLlm);
            var memberUser = CreateMemberPrincipal(10, "andrea@gmail.com", "Andrea Reyes");

            var response = await service.ProcessMessageAsync("When does my membership expire?", memberUser);

            Assert.Equal("LLM.AccountAware", response.Intent);
            Assert.True(response.IsAccountScoped);
            Assert.Contains("Quarterly Pro", stubLlm.LastSystemPrompt);
            Assert.Contains("Jan 30, 2027", stubLlm.LastSystemPrompt);
        }

        [Fact]
        public async Task ProcessMessageAsync_AnonymousUser_DoesNotInjectPersonalMemberData()
        {
            using var context = CreateContext();
            var stubLlm = new StubLlmChatService("Please log in to view your membership expiration date.");
            var service = new ChatbotService(new MemberPortalService(context), new LandingPageService(context), stubLlm);

            var response = await service.ProcessMessageAsync("When does my membership expire?", null);

            Assert.Equal("LLM.General", response.Intent);
            Assert.False(response.IsAccountScoped);
            Assert.Contains("NOT logged in", stubLlm.LastSystemPrompt);
            Assert.DoesNotContain("Jan 30, 2027", stubLlm.LastSystemPrompt);
        }

        [Fact]
        public async Task ProcessMessageAsync_WhenLlmTimesOutOrReturnsNull_UsesSafeFallback()
        {
            using var context = CreateContext();
            var stubLlm = new StubLlmChatService(null);
            var service = new ChatbotService(new MemberPortalService(context), new LandingPageService(context), stubLlm);

            var response = await service.ProcessMessageAsync("What plans do you have?", null);

            Assert.Equal("Fallback", response.Intent);
            Assert.Contains("Quarterly Pro", response.Reply);
            Assert.Contains("4,000", response.Reply);
        }

        [Fact]
        public void ChatbotController_AllowsAnonymousAndDependsOnlyOnIChatbotService()
        {
            var allowAnon = typeof(ChatbotController).GetCustomAttribute<AllowAnonymousAttribute>();
            Assert.NotNull(allowAnon);

            var ctorParams = typeof(ChatbotController)
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Single()
                .GetParameters();

            Assert.Single(ctorParams);
            Assert.Equal(typeof(IChatbotService), ctorParams[0].ParameterType);
        }
    }
}