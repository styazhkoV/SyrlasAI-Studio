using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SyrlasAIEngine.Database;
using SyrlasAIEngine.Services;
using SyrlasAIEngine.Services.LLama;
using SyrlasAIEngine.Services.Parsers;

var builder = WebApplication.CreateBuilder(args);

// 1. Чтение конфигурации из appsettings.json
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                          ?? "Data Source=syrlas_local.db";
string modelPath = builder.Configuration["LlamaSettings:ModelPath"] 
                   ?? "Models/qwen2.5-1.5b-instruct-q4_k_m.gguf";

// 2. Регистрация базы данных (DatabaseInitializer) как синглтона или сscoped
builder.Services.AddSingleton(new DatabaseInitializer(connectionString));

// 3. Регистрация генератора эмбеддингов LLamaSharp как синглтона (тяжелый объект с весами модели в памяти)
builder.Services.AddSingleton<IEmbeddingGenerator>(provider => 
    new LLamaEmbeddingGenerator(modelPath, contextSize: 2048));

// 4. Регистрация парсеров документов (например, PDF, TXT, Markdown)
builder.Services.AddSingleton<IDocumentParser, TextDocumentParser>();
// builder.Services.AddSingleton<IDocumentParser, PdfDocumentParser>(); // при наличии

// 5. Регистрация самого RagService
builder.Services.AddScoped<RagService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 6. Автоматическая инициализация базы данных и таблиц при старте приложения
using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await dbInitializer.InitializeAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();