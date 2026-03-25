using AuntiesCleaners.Client.Models;
using Microsoft.JSInterop;

namespace AuntiesCleaners.Client.Services;

public class PdfService : IPdfService
{
    private readonly IJSRuntime _jsRuntime;

    public PdfService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<byte[]> GenerateBillingReportPdfAsync(BillingReport report)
    {
        var sections = report.Sections.Select(s => new
        {
            name = s.Name,
            lineItems = s.LineItems.Select(li => new { description = li.Description, amount = li.Amount }).ToArray(),
            subtotal = s.Subtotal
        }).ToArray();

        var reportData = new
        {
            dateFrom = report.DateFrom.ToString("MM/dd/yyyy"),
            dateTo = report.DateTo.ToString("MM/dd/yyyy"),
            sections,
            grandTotal = report.GrandTotal
        };

        var base64 = await _jsRuntime.InvokeAsync<string>("pdfInterop.generateBillingReportPdf", reportData);
        return Convert.FromBase64String(base64);
    }
}
