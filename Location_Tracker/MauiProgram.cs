using Location_Tracker.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;

namespace Location_Tracker;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

#if DEBUG
        builder.Services.AddLogging(logging =>
        {
            logging.AddDebug();
        });
#endif

        // Register services
        builder.Services.AddSingleton<LocationService>();
        builder.Services.AddSingleton<DatabaseService>();

        return builder.Build();
    }
}
