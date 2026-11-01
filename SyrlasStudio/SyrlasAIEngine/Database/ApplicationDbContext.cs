using Microsoft.EntityFrameworkCore;
using SyrlasAIEngine.Models;

namespace SyrlasAIEngine.Database;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<DocumentEmbedding> DocumentEmbeddings => Set<DocumentEmbedding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DocumentEmbedding>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DocumentTitle).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Content).IsRequired();

            // Указываем тип колонки vector с размерностью (например, 1536 для OpenAI или совместимых эмбеддингов)
            entity.Property(e => e.Embedding)
                  .HasColumnType("vector(1536)");
        });
    }
}