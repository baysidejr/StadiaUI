using Microsoft.EntityFrameworkCore;
using StadiaUI.Models;
using StadiaUI.Services;

namespace StadiaUI.Data;

public class GameDbContext : DbContext
{
    public DbSet<CachedGame> Games { get; set; }
    public DbSet<GameLibraryCache> LibraryCache { get; set; }

    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CachedGame>()
            .HasIndex(g => g.SteamAppId)
            .IsUnique();
    }
}