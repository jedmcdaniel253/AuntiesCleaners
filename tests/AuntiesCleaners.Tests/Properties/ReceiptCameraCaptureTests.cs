using System.Reflection;
using AuntiesCleaners.Client.Models;
using AuntiesCleaners.Client.Services;
using AuntiesCleaners.Client.Shared;
using Bunit;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;

namespace AuntiesCleaners.Tests.Properties;

/// <summary>
/// Property-based tests for the ReceiptForm camera capture feature.
/// Feature: receipt-camera-capture
/// </summary>
public class ReceiptCameraCaptureTests
{
    // ── Generators ──────────────────────────────────────────────────

    private static Gen<string> BusinessNameGen =>
        Gen.OneOf(
            Gen.Constant(string.Empty),
            Gen.Elements("Home Depot", "Lowes", "Walmart", "Target", "Costco",
                "ACE Hardware", "Dollar General", "Staples", "Best Buy", "Kroger"));

    private static Gen<decimal> AmountGen =>
        Gen.OneOf(
            Gen.Constant(0m),
            Gen.Choose(1, 100000).Select(i => (decimal)i / 100m));

    private static Gen<string> RawTextGen =>
        Gen.Elements("receipt text", "total: $42.00", "thank you", "", "STORE #1234");

    private static Gen<OcrResult> OcrResultGen =>
        from bizName in BusinessNameGen
        from amount in AmountGen
        from rawText in RawTextGen
        select new OcrResult
        {
            BusinessName = bizName,
            Amount = amount,
            RawText = rawText
        };

    // ── Helpers ─────────────────────────────────────────────────────

    private static IBrowserFile CreateMockBrowserFile(byte[]? content = null)
    {
        var fileContent = content ?? new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var file = Substitute.For<IBrowserFile>();
        file.Name.Returns("test-receipt.jpg");
        file.Size.Returns(fileContent.Length);
        file.OpenReadStream(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => new MemoryStream(fileContent));
        return file;
    }

    private static BunitContext CreateTestContext(OcrResult ocrResult)
    {
        var ctx = new BunitContext();

        var ocrService = Substitute.For<IOcrService>();
        ocrService.ExtractTextAsync(Arg.Any<byte[]>())
            .Returns(Task.FromResult(ocrResult));

        var receiptService = Substitute.For<IReceiptService>();
        var snackbar = Substitute.For<ISnackbar>();
        var jsRuntime = Substitute.For<IJSRuntime>();
        var workerService = Substitute.For<IWorkerService>();
        workerService.GetActiveAsync()
            .Returns(Task.FromResult(new List<Worker>()));
        var authService = Substitute.For<IAuthService>();
        authService.GetCurrentWorkerIdAsync()
            .Returns(Task.FromResult((Guid?)null));

        ctx.Services.AddSingleton(ocrService);
        ctx.Services.AddSingleton(receiptService);
        ctx.Services.AddSingleton(snackbar);
        ctx.Services.AddSingleton(jsRuntime);
        ctx.Services.AddSingleton(workerService);
        ctx.Services.AddSingleton(authService);
        ctx.Services.AddMudServices();

        // Setup JS interop stubs for MudBlazor
        ctx.JSInterop.SetupVoid("mudPopover.initialize", _ => true);
        ctx.JSInterop.Setup<int>("mudpopoverHelper.countProviders");
        ctx.JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true);
        ctx.JSInterop.SetupVoid("mudKeyInterceptor.updatekey", _ => true);
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        return ctx;
    }

    /// <summary>
    /// Feature: receipt-camera-capture, Property 1: ProcessFileAsync populates form fields from OCR result
    /// For any valid OcrResult returned by IOcrService.ExtractTextAsync, calling ProcessFileAsync
    /// (via the MudFileUpload FilesChanged event) should set _businessName to the returned BusinessName
    /// when non-empty, and _amount to the returned Amount when > 0.
    /// **Validates: Requirements 2.1, 2.2, 2.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ProcessFilePopulatesFieldsFromOcr()
    {
        return Prop.ForAll(
            Arb.From(OcrResultGen),
            ocrResult =>
            {
                var ctx = CreateTestContext(ocrResult);
                try
                {
                    // Render MudPopoverProvider first, then the component
                    ctx.Render<MudPopoverProvider>();
                    var cut = ctx.Render<ReceiptForm>();

                    // Trigger file selection which calls OnFileSelected -> ProcessFileAsync
                    var mockFile = CreateMockBrowserFile();
                    var fileUpload = cut.FindComponent<MudFileUpload<IBrowserFile>>();
                    cut.InvokeAsync(() => fileUpload.Instance.FilesChanged.InvokeAsync(mockFile));

                    // Read the private fields via reflection to verify the property
                    var componentType = typeof(ReceiptForm);
                    var businessNameField = componentType.GetField("_businessName",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    var amountField = componentType.GetField("_amount",
                        BindingFlags.NonPublic | BindingFlags.Instance);

                    var actualBusinessName = (string)(businessNameField!.GetValue(cut.Instance) ?? string.Empty);
                    var actualAmount = (decimal)(amountField!.GetValue(cut.Instance) ?? 0m);

                    // Verify property: fields match OCR output when applicable
                    var businessNameMatches = string.IsNullOrWhiteSpace(ocrResult.BusinessName)
                        ? actualBusinessName == string.Empty
                        : actualBusinessName == ocrResult.BusinessName;

                    var amountMatches = ocrResult.Amount > 0
                        ? actualAmount == ocrResult.Amount
                        : actualAmount == 0m;

                    return businessNameMatches
                        .Label($"BusinessName: expected '{(string.IsNullOrWhiteSpace(ocrResult.BusinessName) ? "" : ocrResult.BusinessName)}', got '{actualBusinessName}'")
                        .And(amountMatches)
                        .Label($"Amount: expected '{ocrResult.Amount}', got '{actualAmount}'");
                }
                finally
                {
                    ctx.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
            });
    }
}
