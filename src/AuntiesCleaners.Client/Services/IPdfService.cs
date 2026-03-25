using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public interface IPdfService
{
    Task<byte[]> GenerateBillingReportPdfAsync(BillingReport report);
}
