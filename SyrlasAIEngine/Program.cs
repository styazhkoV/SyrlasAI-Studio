using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SyrlasAIEngine.Database;
using SyrlasAIEngine.Services; // <-- добавь это пространство имён
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Добавляем контроллеры и сервисы
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SyrlasAIEngine API", Version = "v1" });
});

// Регистрируем DbContext
builder.Services.AddDbContext<RagDbContext>();

// Регистрируем сервисы
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddScoped<AgentService>(); // <-- регистрация AgentService
builder.Services.AddScoped<RagService>();   // <-- если RagService тоже используется
builder.Services.AddScoped<WorkspaceService>(); // <-- если есть WorkspaceService

var app = builder.Build();

// Автоматически применяем миграции при старте
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

// Настройка middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SyrlasAIEngine API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
