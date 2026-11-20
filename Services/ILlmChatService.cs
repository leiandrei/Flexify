using System.Threading;
using System.Threading.Tasks;

namespace GymCrudApp.Services
{
    public interface ILlmChatService
    {
        Task<string?> GenerateReplyAsync(
            string systemPrompt,
            string userMessage,
            CancellationToken cancellationToken = default);
    }
}