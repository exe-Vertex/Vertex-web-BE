using Microsoft.EntityFrameworkCore;
using Vertex.Entities.Auth;
using Vertex.Entities.Users;

namespace Vertex.Repositories
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
                entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
                entity.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
                entity.Property(x => x.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(500);
                entity.Property(x => x.Role).HasColumnName("role").HasMaxLength(20);
                entity.Property(x => x.Plan).HasColumnName("plan").HasMaxLength(20);
                entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
                entity.Property(x => x.Title).HasColumnName("title").HasMaxLength(100);
                entity.Property(x => x.Bio).HasColumnName("bio");
                entity.Property(x => x.Availability).HasColumnName("availability").HasMaxLength(20);
                entity.Property(x => x.AiQuota).HasColumnName("ai_quota");
                entity.Property(x => x.AiUsed).HasColumnName("ai_used");
                entity.Property(x => x.CreatedAt).HasColumnName("created_at");
                entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

                entity.HasIndex(x => x.Email).IsUnique();
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("refresh_tokens");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.UserId).HasColumnName("user_id");
                entity.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(64);
                entity.Property(x => x.ExpiresAt).HasColumnName("expires_at");
                entity.Property(x => x.CreatedAt).HasColumnName("created_at");
                entity.Property(x => x.RevokedAt).HasColumnName("revoked_at");
                entity.Property(x => x.ReplacedByTokenHash).HasColumnName("replaced_by_token_hash").HasMaxLength(64);

                entity.HasIndex(x => x.TokenHash).IsUnique();
                entity.HasIndex(x => x.UserId);

                entity.HasOne(x => x.User)
                    .WithMany(x => x.RefreshTokens)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
