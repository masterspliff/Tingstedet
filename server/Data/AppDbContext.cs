using Microsoft.EntityFrameworkCore;
using core.Models;

namespace server.Data;

public class AppDbContext : DbContext
{
    public DbSet<Post> Posts { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Vote> Votes { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure one-to-many relationship between Post and Comment
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Post)
            .WithMany(p => p.CommentsList)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure relationships for Vote
        modelBuilder.Entity<Vote>()
            .HasOne(v => v.Post)
            .WithMany(p => p.Votes_Relations)
            .HasForeignKey(v => v.PostId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        modelBuilder.Entity<Vote>()
            .HasOne(v => v.Comment)
            .WithMany(c => c.Votes_Relations)
            .HasForeignKey(v => v.CommentId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}