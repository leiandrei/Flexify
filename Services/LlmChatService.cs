using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GymCrudApp.Services
{
    public class LlmChatService : ILlmChatService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LlmChatService> _logger;

        public LlmChatService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<LlmChatService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string?> GenerateReplyAsync(
            string systemPrompt,
            string userMessage,
            CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["Llm:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Llm:ApiKey is missing. Falling back to offline response.");
                return null;
            }

            var baseUrl = (_configuration["Llm:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/openai/")
                .TrimEnd('/') + "/";
            var model = _configuration["Llm:Model"] ?? "gemini-3.5-flash-lite";
            var timeoutSeconds = int.TryParse(_configuration["Llm:TimeoutSeconds"], out var t) ? t : 8;

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var payload = new
            {
                model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                },
                temperature = 0.3,
                max_tokens = 300
            };

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}chat/completions");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request, timeoutCts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gemini endpoint returned HTTP {StatusCode}.", response.StatusCode);
                    return null;
                }

                var rawJson = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                using var doc = JsonDocument.Parse(rawJson);

                var reply = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return string.IsNullOrWhiteSpace(reply) ? null : reply.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini request failed or timed out. Falling back.");
                return null;
            }
        }
    }
}