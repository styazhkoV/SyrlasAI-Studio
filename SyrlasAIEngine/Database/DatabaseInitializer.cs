using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SyrlasAIEngine.Database
{
    public class DatabaseInitializer
    {
        // Централизованная строка подключения
        public string ConnectionString { get; } = "Data Source=syrlas_ai.db";

        public async Task InitializeAsync()
        {
            using var db = new RagDbContext();
            await db.Database.MigrateAsync();
        }
    }
}
