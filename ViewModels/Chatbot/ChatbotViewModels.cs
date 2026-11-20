using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GymCrudApp.ViewModels.Chatbot
{
    public class ChatRequestViewModel
    {
        [Required(ErrorMessage = "Please enter a message.")]
        [StringLength(500, ErrorMessage = "Message cannot exceed 500 characters.")]
        public string Message { get; set; } = string.Empty;
    }

    public class ChatResponseViewModel
    {
        public string Reply { get; set; } = string.Empty;
        public string Intent { get; set; } = "General";
        public bool IsAccountScoped { get; set; }
        public List<string> SuggestedPrompts { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}