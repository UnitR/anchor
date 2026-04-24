using Anchor.Mobile.Services;
using Anchor.Shared.Storage;

namespace Anchor.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "anchor-phone.db");
        builder.Services.AddSingleton<IAnchorRepository>(_ => new SqliteAnchorRepository(dbPath));
        builder.Services.AddSingleton<VisionValidator>();
        builder.Services.AddSingleton<MotionFreshnessCheck>();
        builder.Services.AddSingleton<FoundationModelsClient>();
        builder.Services.AddSingleton<DesktopConnection>();
        return builder.Build();
    }
}
