using System.Threading.Tasks;
using GymCrudApp.Services;
using GymCrudApp.ViewModels.Chatbot;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrudApp.Controllers
{
    [AllowAnonymous]
    public class ChatbotController : Controller
    {
        private readonly IChatbotService _chatbotService;

        public ChatbotController(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        [HttpGet]
        [HttpGet]
        public IActionResult Prompts()
        {
            return Ok(new { suggestedPrompts = _chatbotService.GetDefaultSuggestedPrompts(User) });
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequestViewModel request)
        {
            if (request == null || !ModelState.IsValid)
            {
                return BadRequest(new ChatResponseViewModel
                {
                    Intent = "ValidationError",
                    Reply = "Please enter a valid question (up to 500 characters)."
                });
            }

            var response = await _chatbotService.ProcessMessageAsync(request.Message, User);
            return Ok(response);
        }
    }
}