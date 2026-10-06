using Microsoft.EntityFrameworkCore;
using OpenHarbor.Plugins.Todo.Models;

namespace OpenHarbor.Plugins.Todo.DAL;

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : DbContext(options)
{
    public DbSet<TodoRecord> Todos => Set<TodoRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoRecord>(entity =>
        {
            entity.HasKey(todo => todo.Id);
            entity.Property(todo => todo.Name).HasMaxLength(256).IsRequired();
            entity.Property(todo => todo.Description).HasMaxLength(10000);
            entity.HasIndex(todo => new { todo.IsFinished, todo.ActivePosition });
            entity.HasIndex(todo => todo.FinishedUtcTicks);
        });
    }
}