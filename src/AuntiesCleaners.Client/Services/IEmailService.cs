namespace AuntiesCleaners.Client.Services;

public interface IEmailService
{
    Task SendBillingReportAsync(string ownerEmail, string subject, byte[] pdfData, List<(string name, byte[] data)> attachments);
}
