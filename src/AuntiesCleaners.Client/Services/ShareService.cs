using Microsoft.JSInterop;

namespace AuntiesCleaners.Client.Services;

public class ShareService : IShareService
{
    private readonly IJSRuntime _jsRuntime;

    public ShareService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<bool> CanShareAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("shareInterop.canShare");
        }
        catch
        {
            return false;
        }
    }

    public async Task ShareTextAsync(string title, string text)
    {
        await _jsRuntime.InvokeVoidAsync("shareInterop.shareText", title, text);
    }

    public async Task ShareWithFileAsync(string title, string text, byte[] fileBytes, string fileName, string mimeType)
    {
        await _jsRuntime.InvokeVoidAsync("shareInterop.shareWithFile", title, text, fileBytes, fileName, mimeType);
    }
}
