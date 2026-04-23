using Anchor.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;

namespace Anchor.iOS;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "anchor-phone.db");
        builder.Services.AddSingleton<IAnchorRepository>(_ => new SqliteAnchorRepository(dbPath));
        builder.Services.AddSingleton<Services.VisionValidator>();
        builder.Services.AddSingleton<Services.MotionFreshnessCheck>();
        builder.Services.AddSingleton<Services.FoundationModelsClient>();
        builder.Services.AddSingleton<Services.DesktopConnection>();
        return builder.Build();
    }
}
