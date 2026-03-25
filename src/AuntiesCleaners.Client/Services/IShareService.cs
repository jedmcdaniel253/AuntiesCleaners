namespace AuntiesCleaners.Client.Services;

public interface IShareService
{
    Task<bool> CanShareAsync();
    Task ShareTextAsync(string title, string text);
}
