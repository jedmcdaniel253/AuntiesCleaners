using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace AuntiesCleaners.Client.Services;

public class OcrService : IOcrService
{
    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _supabaseAnonKey;

    public OcrService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _supabaseUrl = configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url not configured");
        _supabaseAnonKey = configuration["Supabase:AnonKey"] ?? throw new InvalidOperationException("Supabase:AnonKey not configured");
    }

    public async Task<OcrResult> ExtractTextAsync(byte[] imageData)
    {
        try
        {
            var base64 = Convert.ToBase64String(imageData);
            var url = $"{_supabaseUrl}/functions/v1/ocr-receipt";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {_supabaseAnonKey}");
            request.Content = JsonContent.Create(new { image = base64 });

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadFromJsonAsync<OcrResponse>();

            return new OcrResult
            {
                BusinessName = result?.BusinessName ?? string.Empty,
                Amount = result?.Amount ?? 0m,
                RawText = result?.RawText ?? string.Empty
            };
        }
        catch (Exception)
        {
            return new OcrResult();
        }
    }

    private class OcrResponse
    {
        [JsonPropertyName("businessName")]
        public string? BusinessName { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("rawText")]
        public string? RawText { get; set; }
    }
}
