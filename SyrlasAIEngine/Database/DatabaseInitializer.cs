namespace SyrlasAIEngine.Database
{
    public class DatabaseInitializer
    {
        public string ConnectionString { get; } = "Data Source=syrlas_ai.db";

        public async Task InitializeAsync()
        {
            // Логика инициализации таблиц
            await Task.CompletedTask;
        }
    }
}