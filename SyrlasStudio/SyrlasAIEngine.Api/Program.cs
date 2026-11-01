using SyrlasAIEngine.Database;
using SyrlasAIEngine.Services;
using SyrlasAIEngine.Services.Parsers;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=syrlas_local.db";

builder.Services.AddSingleton(new DatabaseInitializer(connectionString));
builder.Services.AddSingleton<IDocumentParser, CodeFileParser>();
builder.Services.AddSingleton<IDocumentParser, OpenXmlOfficeParser>();
builder.Services.AddSingleton<IDocumentParser, PdfPigParser>();
builder.Services.AddSingleton<IDocumentParser, SourceCodeParser>();
builder.Services.AddScoped<RagService>();
builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var databaseInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await databaseInitializer.InitializeAsync();
}

app.MapControllers();
app.Run();
