using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SyrlasAIEngine.Database;
using SyrlasAIEngine.Services;

var builder = WebApplication.CreateBuilder(args);

// Жесткая привязка Kestrel к локальному порту 5000 (без HTTPS для локального IPC)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5000);
});

// Добавляем сервисы
builder.Services.AddControllers();
builder.Services.AddSingleton<RagService>();

// Регистрируем DatabaseInitializer, PromptFactory и AgentService
var dbInitializer = new DatabaseInitializer();
builder.Services.AddSingleton(dbInitializer);
builder.Services.AddSingleton<PromptFactory>();
builder.Services.AddSingleton<AgentService>();
builder.Services.AddSingleton<IDocumentParser, CodeFileParser>();
builder.Services.AddSingleton<IDocumentParser, OpenXmlOfficeParser>();
builder.Services.AddSingleton<IDocumentParser, PdfPigParser>();

// CORS (разрешаем запросы от Electron/Vite)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowElectron", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Инициализация базы данных при старте приложения
await dbInitializer.InitializeAsync();

app.UseCors("AllowElectron");
app.MapControllers();

// Тестовый эндпоинт для проверки связи
app.MapGet("/api/health", () => Results.Ok(new { Status = "Syrlas AI Engine is running" }));

app.Run();