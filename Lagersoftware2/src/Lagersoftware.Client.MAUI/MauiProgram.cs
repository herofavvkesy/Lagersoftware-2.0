using CommunityToolkit.Maui;
using Lagersoftware.Client.Core.Data;
using Lagersoftware.Client.Core.Services;
using Lagersoftware.Client.MAUI.Pages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagersoftware.Client.MAUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<HttpClient>();
        builder.Services.AddSingleton<ApiService>();

        // Local SQLite DB
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Lagersoftware", "local.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        builder.Services.AddDbContext<LocalDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        builder.Services.AddSingleton<SyncService>();

        // Pages
        builder.Services.AddTransient<AppShell>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ProductsPage>();
        builder.Services.AddTransient<ProductDetailPage>();
        builder.Services.AddTransient<StockMovementPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<AdminPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Ensure local DB is created
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
        db.Database.EnsureCreated();

        return app;
    }
}
