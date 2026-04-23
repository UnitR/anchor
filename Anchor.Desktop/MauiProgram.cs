using Anchor.Desktop.Services;
using Anchor.Shared.Scheduling;
using Anchor.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;

namespace Anchor.Desktop;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        var dbDir = Path.Combine(FileSystem.AppDataDirectory, "anchor");
        Directory.CreateDirectory(dbDir);
        var dbPath = Path.Combine(dbDir, "anchor.db");

        builder.Services.AddSingleton<IAnchorRepository>(_ => new SqliteAnchorRepository(dbPath));
        builder.Services.AddSingleton<UltradianScheduler>();
        builder.Services.AddSingleton<LocalPairingService>();
        builder.Services.AddSingleton<ScheduleCoordinator>();
        builder.Services.AddSingleton<IOverlayController>(sp =>
        {
#if MACCATALYST
            return new Platforms.MacCatalyst.MacOverlayController();
#elif WINDOWS
            return new Platforms.Windows.WindowsOverlayController();
#else
            return new NullOverlayController();
#endif
        });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
