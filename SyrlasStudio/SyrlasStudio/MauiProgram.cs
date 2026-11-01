using Microsoft.Extensions.Logging;
using SyrlasStudio.Services;
using SyrlasStudio.ViewModels;

namespace SyrlasStudio
{
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

            // Автоматическое определение базового адреса для эмулятора Android или десктопа
            string baseAddress = DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:5000"
                : "http://localhost:5000";

            // Регистрация HttpClient с таймаутом для LLM-инференса
            builder.Services.AddHttpClient<ApiService>(client =>
            {
                client.BaseAddress = new Uri(baseAddress);
                client.Timeout = TimeSpan.FromMinutes(5);
            });

            // Регистрация архитектурных компонентов MVVM
            builder.Services.AddTransient<MainPageViewModel>();
            builder.Services.AddTransient<MainPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}