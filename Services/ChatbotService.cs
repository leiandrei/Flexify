using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using GymCrudApp.Data;
using GymCrudApp.ViewModels.Chatbot;
using GymCrudApp.ViewModels.MemberPortal;
using Microsoft.EntityFrameworkCore;

namespace GymCrudApp.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly IMemberPortalService _memberPortalService;
        private readonly ILandingPageService _landingPageService;
        private readonly ILlmChatService _llmChatService;
        private readonly GymDbContext? _context;

        public ChatbotService(
            IMemberPortalService memberPortalService,
            ILandingPageService landingPageService,
            ILlmChatService llmChatService,
            GymDbContext? context = null)
        {
            _memberPortalService = memberPortalService;
            _landingPageService = landingPageService;
            _llmChatService = llmChatService;
            _context = context;
        }

        public IReadOnlyList<string> GetDefaultSuggestedPrompts(bool isAuthenticatedMember)
        {
            if (isAuthenticatedMember)
            {
                return new[]
                {
                    "When do I expire?",
                    "Latest payment",
                    "Compare plans"
                };
            }

            return new[]
            {
                "Gym hours",
                "Compare plans",
                "How to renew"
            };
        }

        private static IReadOnlyList<string> GetStaffSuggestedPrompts()
        {
            return new[]
            {
                "Active members",
                "Expiring soon",
                "Today’s check-ins"
            };
        }

        public async Task<ChatResponseViewModel> ProcessMessageAsync(string message, ClaimsPrincipal? user)
        {
            var isAuthenticated = user?.Identity?.IsAuthenticated == true;
            var isMember = isAuthenticated && user!.IsInRole("Member");
            var isStaffOrAdmin = isAuthenticated && (user!.IsInRole("Admin") || user!.IsInRole("Staff"));

            var suggestedPrompts = isStaffOrAdmin
                ? GetStaffSuggestedPrompts().ToList()
                : GetDefaultSuggestedPrompts(isMember).ToList();

            if (string.IsNullOrWhiteSpace(message))
            {
                return new ChatResponseViewModel
                {
                    Intent = "Empty",
                    Reply = "Please enter a question or pick one of the suggested prompts below.",
                    SuggestedPrompts = suggestedPrompts
                };
            }

            var landingData = await _landingPageService.GetLandingPageDataAsync();
            var accountContext = new StringBuilder();
            var isAccountScoped = false;
            MemberPortalViewModel? portal = null;
            AdminSnapshot? adminSnapshot = null;

            if (isStaffOrAdmin && _context != null)
            {
                isAccountScoped = true;
                adminSnapshot = await LoadAdminSnapshotAsync(_context);

                accountContext.AppendLine("AUTHENTICATED ADMIN/STAFF CONTEXT (Verified live gym metrics):");
                accountContext.AppendLine($"- Total Registered Members: {adminSnapshot.TotalMembers} ({adminSnapshot.ActiveMembers} active, {adminSnapshot.InactiveMembers} inactive)");
                accountContext.AppendLine($"- Active Subscriptions: {adminSnapshot.ActiveSubscriptions}");
                accountContext.AppendLine($"- Expiring Within 7 Days ({adminSnapshot.ExpiringCount}): {(adminSnapshot.ExpiringNames.Any() ? string.Join(", ", adminSnapshot.ExpiringNames) : "None")}");
                accountContext.AppendLine($"- Revenue This Month: ₱{adminSnapshot.MonthlyRevenue:N2}");
                accountContext.AppendLine($"- Today's Check-Ins: {adminSnapshot.TodayCheckIns} ({adminSnapshot.CurrentlyOnFloor} currently checked in on the gym floor)");
            }
            else if (isMember && user != null)
            {
                int? userId = int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedId)
                    ? parsedId
                    : null;
                var email = user.FindFirstValue(ClaimTypes.Email);

                portal = await _memberPortalService.GetPortalForUserAsync(userId, email);
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
                accountContext.AppendLine("AUTHENTICATED MEMBER CONTEXT: User is NOT logged in as a Member or Staff. If they ask about personal subscriptions or internal gym statistics, instruct them to log in first.");
            }

            var plansText = landingData.Plans.Any()
                ? string.Join("\n", landingData.Plans.Select(p =>
                    $"- {p.PlanName}: ₱{p.PlanPrice:N0} · {p.DurationDays} {(p.DurationDays == 1 ? "day" : "days")}"))
                : "- No active plans published online.";

            var systemPrompt = $@"
You are the virtual front-desk assistant for {landingData.FacilityName}.
Be accurate and polite. Use ONLY the verified facts below. Never invent prices, policies, or personal data.

FORMAT (important):
- Prefer short lines and bullet lists over long paragraphs.
- For plans, hours, or multiple facts: use a one-line intro, then markdown bullets like ""- Item"".
- Put each plan on its own bullet: ""- Plan name: ₱price · N days"".
- Keep replies scannable. Avoid stuffing many facts into one sentence with semicolons.
- Do not use tables or HTML.

FACILITY INFO:
- Address: {landingData.FacilityAddress}
- Operating Hours: {landingData.OperatingHours}
- Contact: {landingData.ContactPhone} | {landingData.ContactEmail}
- Active Membership Plans:
{plansText}
- Accepted Payments: Cash, GCash, Maya, Bank Transfer, and Card (non-cash payments require a reference number).
- Renewal Rule: Early renewals start the day after the current expiration date so no paid days are lost; expired plans start on the renewal date.
- Amenities & Rules: Day-use lockers, showers, free member Wi-Fi, 2-hour validated parking, minimum age 16 (parental consent for 16-17), closed-toe athletic shoes required.

{accountContext}";

            var trimmedMessage = message.Trim();
            var llmReply = await _llmChatService.GenerateReplyAsync(systemPrompt, trimmedMessage);

            if (!string.IsNullOrWhiteSpace(llmReply))
            {
                return new ChatResponseViewModel
                {
                    Intent = isAccountScoped ? "LLM.AccountAware" : "LLM.General",
                    IsAccountScoped = isAccountScoped,
                    Reply = llmReply,
                    SuggestedPrompts = suggestedPrompts
                };
            }

            var (_, fallbackScoped, fallbackReply) =
                BuildSmartFallbackReply(trimmedMessage, isMember, isStaffOrAdmin, portal, adminSnapshot, landingData, plansText);

            return new ChatResponseViewModel
            {
                Intent = "Fallback",
                IsAccountScoped = fallbackScoped,
                Reply = fallbackReply,
                SuggestedPrompts = suggestedPrompts
            };
        }

        private static async Task<AdminSnapshot> LoadAdminSnapshotAsync(GymDbContext context)
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var sevenDaysOut = today.AddDays(7);

            var totalMembers = await context.Members.CountAsync();
            var activeMembers = await context.Members.CountAsync(m => m.isActive);

            var activeSubscriptions = await context.Memberships
                .CountAsync(ms => ms.isActive && ms.endDate.Date >= today);

            var expiringList = await context.Memberships
                .AsNoTracking()
                .Include(ms => ms.Member)
                .Include(ms => ms.Plan)
                .Where(ms => ms.isActive && ms.endDate.Date >= today && ms.endDate.Date <= sevenDaysOut)
                .OrderBy(ms => ms.endDate)
                .Select(ms => new
                {
                    Name = ms.Member != null ? ms.Member.fullName : $"Member #{ms.memberId}",
                    EndDate = ms.endDate
                })
                .ToListAsync();

            var monthlyRevenue = await context.Payments
                .Where(p => !p.isVoided && p.paymentDate >= monthStart)
                .SumAsync(p => (decimal?)p.amount) ?? 0m;

            var todayCheckIns = await context.Attendance
                .CountAsync(a => a.checkIn >= today && a.checkIn < today.AddDays(1));

            var currentlyOnFloor = await context.Attendance
                .CountAsync(a => a.checkIn >= today && a.checkOut == null);

            return new AdminSnapshot(
                totalMembers,
                activeMembers,
                totalMembers - activeMembers,
                activeSubscriptions,
                expiringList.Count,
                expiringList.Select(x => $"{x.Name} (expires {x.EndDate:MMM dd})").ToList(),
                monthlyRevenue,
                todayCheckIns,
                currentlyOnFloor);
        }

        private static (string Intent, bool IsAccountScoped, string Reply) BuildSmartFallbackReply(
            string message,
            bool isMember,
            bool isStaffOrAdmin,
            MemberPortalViewModel? portal,
            AdminSnapshot? adminSnapshot,
            dynamic landingData,
            string plansText)
        {
            var q = message.ToLowerInvariant();

            // 1. Admin / Staff operational queries
            if (isStaffOrAdmin && adminSnapshot != null)
            {
                if (q.Contains("expir") || q.Contains("due") || q.Contains("renew soon"))
                {
                    if (adminSnapshot.ExpiringCount == 0)
                    {
                        return ("Fallback.AdminMetrics", true, "There are no active memberships expiring within the next 7 days.");
                    }

                    return ("Fallback.AdminMetrics", true,
                        $"There {(adminSnapshot.ExpiringCount == 1 ? "is" : "are")} {adminSnapshot.ExpiringCount} {(adminSnapshot.ExpiringCount == 1 ? "membership" : "memberships")} expiring within 7 days: {string.Join(", ", adminSnapshot.ExpiringNames)}.");
                }

                if (q.Contains("revenue") || q.Contains("income") || q.Contains("sales") || q.Contains("earn") || q.Contains("collection"))
                {
                    return ("Fallback.AdminMetrics", true,
                        $"Total non-voided revenue collected so far this month is ₱{adminSnapshot.MonthlyRevenue:N2}.");
                }

                if (q.Contains("check-in") || q.Contains("check in") || q.Contains("attendance") || q.Contains("floor") || q.Contains("visit"))
                {
                    return ("Fallback.AdminMetrics", true,
                        $"There have been {adminSnapshot.TodayCheckIns} check-ins today, with {adminSnapshot.CurrentlyOnFloor} {(adminSnapshot.CurrentlyOnFloor == 1 ? "member" : "members")} currently on the gym floor.");
                }

                if (q.Contains("member") || q.Contains("how many") || q.Contains("count") || q.Contains("active"))
                {
                    return ("Fallback.AdminMetrics", true,
                        $"You currently have {adminSnapshot.TotalMembers} registered members ({adminSnapshot.ActiveMembers} active, {adminSnapshot.InactiveMembers} inactive) and {adminSnapshot.ActiveSubscriptions} active subscriptions.");
                }
            }

            // 2. Account-scoped member intents
            if (q.Contains("expire") || q.Contains("expiry") || q.Contains("subscription") || q.Contains("my plan") || q.Contains("my membership") || q.Contains("days remaining"))
            {
                if (!isMember || portal == null)
                {
                    return ("Fallback.AuthRequired", false, "Please log in to your Member account so I can check your personal membership status and expiration date.");
                }

                if (!portal.ExpiryDate.HasValue || string.IsNullOrWhiteSpace(portal.PlanName))
                {
                    return ("Fallback.AccountAware", true, "You do not have an active membership plan on file yet. Visit the front desk or check our available plans to get started.");
                }

                var days = Math.Max(0, portal.DaysRemaining);
                return ("Fallback.AccountAware", true,
                    $"Your {portal.PlanName} membership is currently {portal.MembershipStatus} and expires on {portal.ExpiryDate.Value:MMM dd, yyyy} ({days} {(days == 1 ? "day" : "days")} remaining).");
            }

            if (q.Contains("payment") && (q.Contains("my") || q.Contains("latest") || q.Contains("last") || q.Contains("receipt") || q.Contains("history")))
            {
                if (!isMember || portal == null)
                {
                    return ("Fallback.AuthRequired", false, "Please log in to your Member account to view your payment history.");
                }

                var latestPayment = portal.Payments?.FirstOrDefault(p => !p.IsVoided);
                if (latestPayment == null)
                {
                    return ("Fallback.AccountAware", true, "No completed payments were found on your account yet.");
                }

                var refText = string.IsNullOrWhiteSpace(latestPayment.ReferenceNumber) ? "N/A" : latestPayment.ReferenceNumber;
                return ("Fallback.AccountAware", true,
                    $"Your latest recorded payment is ₱{latestPayment.Amount:N2} via {latestPayment.PaymentType} on {latestPayment.PaymentDate:MMM dd, yyyy} (Ref: {refText}).");
            }

            if (q.Contains("check-in") || q.Contains("check in") || q.Contains("checked in") || q.Contains("visit") || q.Contains("attendance"))
            {
                if (!isMember || portal == null)
                {
                    return ("Fallback.AuthRequired", false, "Please log in to your Member account to view your gym visit and check-in history.");
                }

                var count = portal.AttendanceLogs?.Count ?? 0;
                var latest = portal.AttendanceLogs?.FirstOrDefault();
                if (count == 0 || latest == null)
                {
                    return ("Fallback.AccountAware", true, "You have no recorded gym check-ins yet.");
                }

                return ("Fallback.AccountAware", true,
                    $"You have {count} recent {(count == 1 ? "check-in" : "check-ins")} logged. Your latest visit was on {latest.CheckIn:MMM dd, yyyy 'at' h:mm tt} ({latest.DurationDisplay}).");
            }

            // 3. Facility & public FAQ intents
            if (q.Contains("hour") || q.Contains("open") || q.Contains("close") || q.Contains("schedule") || q.Contains("time"))
            {
                return ("Fallback.Hours", false,
                    $"Here’s when we’re open:\n- {landingData.OperatingHours}\n\nCall or text {landingData.ContactPhone} if you need anything else.");
            }

            if (q.Contains("pay") || q.Contains("gcash") || q.Contains("maya") || q.Contains("cash") || q.Contains("card") || q.Contains("bank"))
            {
                return ("Fallback.PaymentMethods", false,
                    "We accept:\n- Cash\n- GCash\n- Maya\n- Bank Transfer\n- Card\n\nNon-cash payments need a reference number at checkout.");
            }

            if (q.Contains("renew") || q.Contains("extend"))
            {
                return ("Fallback.Renewal", false,
                    "How renewal works:\n- Early renewals start the day after your current end date (no lost days).\n- Expired plans start on the renewal date.");
            }

            if (q.Contains("plan") || q.Contains("price") || q.Contains("rate") || q.Contains("cost") || q.Contains("fee") || q.Contains("offer"))
            {
                return ("Fallback.Plans", false, $"Here are the current plans:\n{plansText}");
            }

            return ("Fallback.General", false,
                $"Here’s the quick overview:\n\nPlans:\n{plansText}\n\nHours:\n- {landingData.OperatingHours}\n\nContact:\n- {landingData.ContactPhone}");
        }
        
        public IReadOnlyList<string> GetDefaultSuggestedPrompts(ClaimsPrincipal user)
        {
            var isAuthenticated = user?.Identity?.IsAuthenticated == true;
            if (isAuthenticated && (user!.IsInRole("Admin") || user!.IsInRole("Staff")))
            {
                return GetStaffSuggestedPrompts();
            }

            var isMember = isAuthenticated && user!.IsInRole("Member");
            return GetDefaultSuggestedPrompts(isMember);
        }

        private sealed record AdminSnapshot(
            int TotalMembers,
            int ActiveMembers,
            int InactiveMembers,
            int ActiveSubscriptions,
            int ExpiringCount,
            IReadOnlyList<string> ExpiringNames,
            decimal MonthlyRevenue,
            int TodayCheckIns,
            int CurrentlyOnFloor);
    }
}