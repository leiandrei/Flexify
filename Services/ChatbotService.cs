using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using GymCrudApp.ViewModels.Chatbot;

namespace GymCrudApp.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly IMemberPortalService _memberPortalService;
        private readonly ILandingPageService _landingPageService;
        private readonly ILlmChatService _llmChatService;

        public ChatbotService(
            IMemberPortalService memberPortalService,
            ILandingPageService landingPageService,
            ILlmChatService llmChatService)
        {
            _memberPortalService = memberPortalService;
            _landingPageService = landingPageService;
            _llmChatService = llmChatService;
        }

        public IReadOnlyList<string> GetDefaultSuggestedPrompts(bool isAuthenticatedMember)
        {
            if (isAuthenticatedMember)
            {
                return new[]
                {
                    "When does my membership expire?",
                    "Show my latest payment",
                    "How many times have I checked in recently?",
                    "What membership plans do you offer?"
                };
            }

            return new[]
            {
                "What membership plans do you offer?",
                "What are your operating hours?",
                "What payment methods do you accept?",
                "How does membership renewal work?"
            };
        }

        public async Task<ChatResponseViewModel> ProcessMessageAsync(string message, ClaimsPrincipal? user)
        {
            var isMember = user?.Identity?.IsAuthenticated == true && user.IsInRole("Member");

            if (string.IsNullOrWhiteSpace(message))
            {
                return new ChatResponseViewModel
                {
                    Intent = "Empty",
                    Reply = "Please enter a question or pick one of the suggested prompts below.",
                    SuggestedPrompts = GetDefaultSuggestedPrompts(isMember).ToList()
                };
            }

            var landingData = await _landingPageService.GetLandingPageDataAsync();
            var accountContext = new StringBuilder();
            var isAccountScoped = false;

            if (isMember && user != null)
            {
                int? userId = int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedId)
                    ? parsedId
                    : null;
                var email = user.FindFirstValue(ClaimTypes.Email);

                var portal = await _memberPortalService.GetPortalForUserAsync(userId, email);
                if (portal != null)
                {
                    isAccountScoped = true;
                    accountContext.AppendLine("AUTHENTICATED MEMBER CONTEXT (Verified from session claims):");
                    accountContext.AppendLine($"- Plan: {portal.PlanName ?? "None"} | Status: {portal.MembershipStatus}");
                    if (portal.ExpiryDate.HasValue)
                    {
                        accountContext.AppendLine($"- Expiration Date: {portal.ExpiryDate.Value:MMM dd, yyyy} ({Math.Max(0, portal.DaysRemaining)} days remaining)");
                    }

                    var latestPayment = portal.Payments?.FirstOrDefault(p => !p.IsVoided);
                    if (latestPayment != null)
                    {
                        accountContext.AppendLine($"- Latest Payment: ₱{latestPayment.Amount:N2} via {latestPayment.PaymentType} on {latestPayment.PaymentDate:MMM dd, yyyy} (Ref: {latestPayment.ReferenceNumber ?? "N/A"})");
                    }

                    var checkInCount = portal.AttendanceLogs?.Count ?? 0;
                    accountContext.AppendLine($"- Recent Check-Ins Logged: {checkInCount}");
                    var latestCheckIn = portal.AttendanceLogs?.FirstOrDefault();
                    if (latestCheckIn != null)
                    {
                        accountContext.AppendLine($"- Latest Check-In: {latestCheckIn.CheckIn:MMM dd, yyyy 'at' h:mm tt} ({latestCheckIn.DurationDisplay})");
                    }
                }
            }
            else
            {
                accountContext.AppendLine("AUTHENTICATED MEMBER CONTEXT: User is NOT logged in as a Member. If they ask about their personal subscription, payments, or attendance, instruct them to log in to their Member account.");
            }

            var plansText = landingData.Plans.Any()
                ? string.Join("; ", landingData.Plans.Select(p => $"{p.PlanName} = ₱{p.PlanPrice:N2} for {p.DurationDays} days"))
                : "No active plans published online.";

            var systemPrompt = $@"
You are the virtual front-desk assistant for {landingData.FacilityName}.
Answer concisely (2-4 sentences max), accurately, and politely using ONLY the verified facts below. Never invent prices, policies, or personal data.

FACILITY INFO:
- Address: {landingData.FacilityAddress}
- Operating Hours: {landingData.OperatingHours}
- Contact: {landingData.ContactPhone} | {landingData.ContactEmail}
- Active Membership Plans: {plansText}
- Accepted Payments: Cash, GCash, Maya, Bank Transfer, and Card (non-cash payments require a reference number).
- Renewal Rule: Early renewals start the day after the current expiration date so no paid days are lost; expired plans start on the renewal date.
- Amenities & Rules: Day-use lockers, showers, free member Wi-Fi, 2-hour validated parking, minimum age 16 (parental consent for 16-17), closed-toe athletic shoes required.

{accountContext}";

            var llmReply = await _llmChatService.GenerateReplyAsync(systemPrompt, message.Trim());

            if (!string.IsNullOrWhiteSpace(llmReply))
            {
                return new ChatResponseViewModel
                {
                    Intent = isAccountScoped ? "LLM.AccountAware" : "LLM.General",
                    IsAccountScoped = isAccountScoped,
                    Reply = llmReply,
                    SuggestedPrompts = GetDefaultSuggestedPrompts(isMember).ToList()
                };
            }

            return new ChatResponseViewModel
            {
                Intent = "Fallback",
                IsAccountScoped = false,
                Reply = $"Our assistant is temporarily in offline mode. Current plans: {plansText}. Operating hours: {landingData.OperatingHours}. Contact: {landingData.ContactPhone}.",
                SuggestedPrompts = GetDefaultSuggestedPrompts(isMember).ToList()
            };
        }
    }
}