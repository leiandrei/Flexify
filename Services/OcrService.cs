using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GymCrudApp.Services
{
    public class OcrService : IOcrService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _endpoint;

        public OcrService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Ocr:ApiKey"] ?? string.Empty;
            _endpoint = configuration["Ocr:Endpoint"] ?? "https://api.ocr.space/parse/image";
        }

        public async Task<string> ExtractRawTextAsync(
            byte[] fileBytes,
            string mimeType,
            CancellationToken cancellationToken = default)
        {
            if (fileBytes == null || fileBytes.Length == 0 || string.IsNullOrWhiteSpace(_apiKey))
            {
                return string.Empty;
            }

            var fileName = mimeType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                ? "receipt.pdf"
                : "receipt.png";

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(_apiKey), "apikey");
            form.Add(new StringContent("eng"), "language");
            form.Add(new StringContent("2"), "OCREngine");

            var byteContent = new ByteArrayContent(fileBytes);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType);
            form.Add(byteContent, "file", fileName);

            using var response = await _httpClient.PostAsync(_endpoint, form, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("IsErroredOnProcessing", out var errored) && errored.GetBoolean())
            {
                return string.Empty;
            }

            if (!doc.RootElement.TryGetProperty("ParsedResults", out var results) || results.GetArrayLength() == 0)
            {
                return string.Empty;
            }

            return string.Join(
                Environment.NewLine,
                results.EnumerateArray()
                    .Select(r => r.TryGetProperty("ParsedText", out var pt) ? pt.GetString() ?? string.Empty : string.Empty))
                .Trim();
        }
    }
}