using Microsoft.EntityFrameworkCore;
using SyrlasAIEngine.Models;

namespace SyrlasAIEngine.Database
{
    public class RagDbContext : DbContext
    {
        public DbSet<DocumentChunk> DocumentChunks { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=syrlas_ai.db");
        }
    }
}
