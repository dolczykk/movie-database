using Microsoft.EntityFrameworkCore;

using MovieDatabase.Api.Core.Documents.Films;
using MovieDatabase.Api.Core.Documents.Users;

namespace MovieDatabase.Api.Infrastructure.Db;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Film> Films { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Film>(entity =>
        {
            entity.ToContainer("Film");
            entity.HasPartitionKey(f => f.Title);
            entity.HasKey(f => f.Id);

            entity.OwnsOne(f => f.Director, director =>
            {
                director.HasKey(d => d.Id);
            });

            entity.OwnsOne(f => f.Producer, producer =>
            {
                producer.HasKey(p => p.Id);
            });

            entity.OwnsMany(f => f.Actors, actor =>
            {
                actor.HasKey(a => a.Id);
            });

            entity.OwnsMany(f => f.Genres, genre =>
            {
                genre.HasKey(g => g.Id);
            });

            entity.Property(f => f.Id).ToJsonProperty("id");
            entity.Property(f => f.Title).IsRequired();
            entity.Property(f => f.ReleaseDate).HasConversion(
                v => v.ToDateTime(TimeOnly.MinValue),
                v => DateOnly.FromDateTime(v));

            entity.HasQueryFilter(f => !f.IsDeleted);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToContainer("User");
            entity.HasPartitionKey(u => u.Email);
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Id).ToJsonProperty("id");
            entity.Property(u => u.Email).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();

            entity.HasQueryFilter(f => !f.IsDeleted);
        });
    }
}