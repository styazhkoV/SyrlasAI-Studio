global using SyrlasAIEngine.Services;
using Microsoft.Extensions.Logging;
using SyrlasStudio.ViewModels;
using SyrlasStudio.Services;

namespace SyrlasStudio;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<AgentService>();
        builder.Services.AddSingleton<ResourceMonitorService>();
        builder.Services.AddTransient<MainPageViewModel>();
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}
