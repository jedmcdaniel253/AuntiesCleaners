namespace AuntiesCleaners.Client.Services;

public interface IShareService
{
    Task<bool> CanShareAsync();
    Task ShareTextAsync(string title, string text);
    Task ShareWithFileAsync(string title, string text, byte[] fileBytes, string fileName, string mimeType);
}
