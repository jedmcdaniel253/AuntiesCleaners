namespace AuntiesCleaners.Client.Services;

public class OcrResult
{
    public string BusinessName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string RawText { get; set; } = string.Empty;
}

public interface IOcrService
{
    Task<OcrResult> ExtractTextAsync(byte[] imageData);
}
