using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using AuntiesCleaners.Client;
using AuntiesCleaners.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();
builder.Services.AddSingleton<ISupabaseClientService, SupabaseClientService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITabConfigService, TabConfigService>();
builder.Services.AddScoped<IWorkerService, WorkerService>();
builder.Services.AddScoped<IHouseService, HouseService>();
builder.Services.AddScoped<IWorkEntryService, WorkEntryService>();
builder.Services.AddScoped<IOcrService, OcrService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();
builder.Services.AddScoped<IShareService, ShareService>();
builder.Services.AddScoped<IMiscEntryService, MiscEntryService>();
builder.Services.AddScoped<IRateService, RateService>();
builder.Services.AddScoped<ITurnoverService, TurnoverService>();
builder.Services.AddScoped<IMowListService, MowListService>();
builder.Services.AddScoped<IBillingReportService, BillingReportService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IOwnerService, OwnerService>();
builder.Services.AddScoped<IWorkerPayService, WorkerPayService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<TabConfigNotifier>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
