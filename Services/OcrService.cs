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
    public class OcrService : IOcrService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OcrService> _logger;

        public OcrService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<OcrService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> ExtractRawTextAsync(
            byte[] fileBytes,
            string mimeType,
            CancellationToken cancellationToken = default)
        {
            if (fileBytes == null || fileBytes.Length == 0)
                return string.Empty;

            var apiKey = _configuration["Llm:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Llm:ApiKey is missing. Returning empty OCR text for manual entry.");
                return string.Empty;
            }

            var baseUrl = (_configuration["Llm:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/openai/")
                .TrimEnd('/') + "/";
            var model = _configuration["Llm:Model"] ?? "gemini-3.5-flash-lite";
            var timeoutSeconds = int.TryParse(_configuration["Llm:TimeoutSeconds"], out var t) ? Math.Max(t, 12) : 12;

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var base64Data = Convert.ToBase64String(fileBytes);
            var dataUrl = $"data:{mimeType};base64,{base64Data}";

            var payload = new
            {
                model,
                messages = new object[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "text",
                                text = "Perform verbatim OCR on this payment receipt. Return ONLY the exact raw text visible on the receipt, preserving line breaks. Do not summarize, explain, or format as markdown."
                            },
                            new
                            {
                                type = "image_url",
                                image_url = new { url = dataUrl }
                            }
                        }
                    }
                },
                temperature = 0.0,
                max_tokens = 600
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
                    _logger.LogWarning("OCR endpoint returned HTTP {StatusCode}.", response.StatusCode);
                    return string.Empty;
                }

                var rawJson = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                using var doc = JsonDocument.Parse(rawJson);

                var text = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OCR extraction failed or timed out. Falling back to blank form.");
                return string.Empty;
            }
        }
    }
}