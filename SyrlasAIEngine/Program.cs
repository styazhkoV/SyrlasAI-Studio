using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SyrlasAIEngine.Database;
using SyrlasAIEngine.Services;
using SyrlasAIEngine.Services.Parsers;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5000);
});

builder.Services.AddControllers();

// Базовые сервисы
var dbInitializer = new DatabaseInitializer();
builder.Services.AddSingleton(dbInitializer);
builder.Services.AddSingleton<PromptFactory>();
builder.Services.AddSingleton<AgentService>();

// Парсеры документов и кода
builder.Services.AddSingleton<IDocumentParser, CodeFileParser>();
builder.Services.AddSingleton<IDocumentParser, SourceCodeParser>(); // Поддержка C, C++, C#
builder.Services.AddSingleton<IDocumentParser, OpenXmlOfficeParser>();
builder.Services.AddSingleton<IDocumentParser, PdfPigParser>();

// RAG и LlamaSharp Инференс
builder.Services.AddSingleton<RagService>();
builder.Services.AddSingleton<LlamaInferenceService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowElectron", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

await dbInitializer.InitializeAsync();

app.UseCors("AllowElectron");
app.MapControllers();

app.MapGet("/api/health", (LlamaInferenceService llama) => Results.Ok(new 
{ 
    Status = "Syrlas AI Engine is running",
    LlamaLoaded = llama.IsLoaded
}));

app.Run();