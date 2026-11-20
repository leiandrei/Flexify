using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using GymCrudApp.ViewModels.Chatbot;

namespace GymCrudApp.Services
{
    public interface IChatbotService
    {
        Task<ChatResponseViewModel> ProcessMessageAsync(string message, ClaimsPrincipal? user);
        IReadOnlyList<string> GetDefaultSuggestedPrompts(bool isAuthenticatedMember);
    }
}