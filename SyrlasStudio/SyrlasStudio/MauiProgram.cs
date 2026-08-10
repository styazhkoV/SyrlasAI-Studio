using Microsoft.Extensions.Logging;
using SyrlasStudio.ViewModels;
using SyrlasStudio.Services;
using SyrlasAIEngine.Services;

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

        // Регистрация сервисов AI-движка из библиотеки SyrlasAIEngine
        builder.Services.AddSingleton<LlamaInferenceService>(); //[cite: 28]
        builder.Services.AddSingleton<PromptFactory>();         //[cite: 29]
        builder.Services.AddSingleton<RagService>();            //[cite: 30]
        builder.Services.AddTransient<AgentService>();          //[cite: 27]
        builder.Services.AddSingleton<ResourceMonitorService>();
        builder.Services.AddTransient<MainPageViewModel>();
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}