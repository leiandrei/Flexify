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

            var configuredBaseUrl = _configuration["Llm:BaseUrl"];
            var baseUrl = (string.IsNullOrWhiteSpace(configuredBaseUrl)
                ? "https://generativelanguage.googleapis.com/v1beta/openai/"
                : configuredBaseUrl).TrimEnd('/') + "/";

            var configuredModel = _configuration["Llm:Model"];
            var primaryModel = string.IsNullOrWhiteSpace(configuredModel)
                ? "gemini-3.1-flash-lite"
                : configuredModel.Trim();

            var fallbackModel = _configuration["Llm:FallbackModel"]?.Trim();
            var candidateModels = string.IsNullOrWhiteSpace(fallbackModel) || fallbackModel == primaryModel
                ? new[] { primaryModel }
                : new[] { primaryModel, fallbackModel };

            var timeoutSeconds = int.TryParse(_configuration["Llm:TimeoutSeconds"], out var t) && t > 0 ? t : 15;

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            foreach (var model in candidateModels)
            {
                for (var attempt = 1; attempt <= 2; attempt++)
                {
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
                        if (response.IsSuccessStatusCode)
                        {
                            var rawJson = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                            using var doc = JsonDocument.Parse(rawJson);

                            var reply = doc.RootElement
                                .GetProperty("choices")[0]
                                .GetProperty("message")
                                .GetProperty("content")
                                .GetString();

                            if (!string.IsNullOrWhiteSpace(reply))
                            {
                                return reply.Trim();
                            }
                        }

                        var statusCode = (int)response.StatusCode;
                        var errBody = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                        _logger.LogWarning(
                            "Gemini endpoint ({Model}, attempt {Attempt}) returned HTTP {StatusCode}: {ErrorBody}",
                            model, attempt, response.StatusCode, errBody);

                        // Retry only on transient 429/503 capacity errors
                        if ((statusCode == 429 || statusCode == 503) && attempt == 1)
                        {
                            await Task.Delay(700, timeoutCts.Token);
                            continue;
                        }

                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Gemini request ({Model}, attempt {Attempt}) failed or timed out.", model, attempt);
                        break;
                    }
                }
            }

            return null;
        }
    }
}