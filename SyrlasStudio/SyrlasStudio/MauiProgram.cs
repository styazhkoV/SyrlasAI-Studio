global using SyrlasAIEngine.Services;
using Microsoft.Extensions.Logging;
using SyrlasStudio.ViewModels;
using SyrlasStudio.Services;
using SyrlasAIEngine;

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
        System.Diagnostics.Debugger.Launch();
#endif

        // Регистрация сервисов
        builder.Services.AddSingleton<LlamaInferenceService>();
        builder.Services.AddSingleton<AgentService>();

        // Регистрация ViewModel и страниц
        builder.Services.AddTransient<MainPageViewModel>();
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}