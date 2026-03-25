using Microsoft.JSInterop;

namespace AuntiesCleaners.Client.Services;

public class OcrService : IOcrService
{
    private readonly IJSRuntime _jsRuntime;

    public OcrService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<OcrResult> ExtractTextAsync(byte[] imageData)
    {
        try
        {
            var base64 = Convert.ToBase64String(imageData);
            var result = await _jsRuntime.InvokeAsync<OcrJsResult>("ocrInterop.extractText", base64);

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

    private class OcrJsResult
    {
        public string? BusinessName { get; set; }
        public decimal Amount { get; set; }
        public string? RawText { get; set; }
    }
}
