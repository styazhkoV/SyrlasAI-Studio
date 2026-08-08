using Microsoft.Extensions.Logging;
using SyrlasAIEngine.Database;
using SyrlasAIEngine.Services;
using SyrlasStudio.ViewModels;

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

        // =========================================================
        // 1. Регистрация сервисов ядра (SyrlasAIEngine)
        // =========================================================

        // Singleton: Инференс LLM и база данных должны жить в течение всего
        // жизненного цикла приложения, чтобы не перегружать модель в VRAM/RAM.
        builder.Services.AddSingleton<DatabaseInitializer>();
        builder.Services.AddSingleton<LlamaInferenceService>();

        // Transient: Сервисы логики создаются по требованию
        builder.Services.AddTransient<RagService>();
        builder.Services.AddTransient<AgentService>();

        // =========================================================
        // 2. Регистрация слоя представления и ViewModels (SyrlasStudio)
        // =========================================================

        builder.Services.AddSingleton<MainPageViewModel>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}