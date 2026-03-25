using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace AuntiesCleaners.Client.Services;

public class EmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _supabaseAnonKey;

    public EmailService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _supabaseUrl = configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url not configured");
        _supabaseAnonKey = configuration["Supabase:AnonKey"] ?? throw new InvalidOperationException("Supabase:AnonKey not configured");
    }

    public async Task SendBillingReportAsync(string ownerEmail, string subject, byte[] pdfData, List<(string name, byte[] data)> attachments)
    {
        var allAttachments = new List<object>
        {
            new { filename = "BillingReport.pdf", content = Convert.ToBase64String(pdfData) }
        };

        foreach (var (name, data) in attachments)
        {
            allAttachments.Add(new { filename = name, content = Convert.ToBase64String(data) });
        }

        var body = $"<h1>Auntie's Cleaners — Billing Report</h1><p>Please find the billing report attached.</p>";

        var payload = new
        {
            to = ownerEmail,
            subject,
            body,
            attachments = allAttachments
        };

        var url = $"{_supabaseUrl}/functions/v1/send-email";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Authorization", $"Bearer {_supabaseAnonKey}");
        request.Content = JsonContent.Create(payload);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
