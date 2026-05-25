using Microsoft.EntityFrameworkCore;
using Vertex.Entities.Auth;
using Vertex.Entities.Notifications;
using Vertex.Entities.Organizations;
using Vertex.Entities.Projects;
using Vertex.Entities.Users;
using Vertex.Entities.Workspaces;

namespace Vertex.Repositories
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
        public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
        public DbSet<TaskComment> TaskComments => Set<TaskComment>();
        public DbSet<Workspace> Workspaces => Set<Workspace>();
        public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
        public DbSet<Notification> Notifications => Set<Notification>();

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
                entity.Property(x => x.Role).HasColumnName("role").HasMaxLength(20).HasDefaultValue("member");
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

            modelBuilder.Entity<Organization>(entity =>
            {
                entity.ToTable("organizations");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
                entity.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(100);
                entity.Property(x => x.Plan).HasColumnName("plan").HasMaxLength(20).HasDefaultValue("free");
                entity.Property(x => x.MaxMembers).HasColumnName("max_members");
                entity.Property(x => x.AiQuota).HasColumnName("ai_quota");
                entity.Property(x => x.StorageLimit).HasColumnName("storage_limit");
                entity.Property(x => x.CreatedAt).HasColumnName("created_at");
                entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

                entity.HasIndex(x => x.Slug).IsUnique();
            });

            modelBuilder.Entity<OrganizationMember>(entity =>
            {
                entity.ToTable("organization_members");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.OrgId).HasColumnName("org_id");
                entity.Property(x => x.UserId).HasColumnName("user_id");
                entity.Property(x => x.Role).HasColumnName("role").HasMaxLength(20).HasDefaultValue("member");
                entity.Property(x => x.JoinedAt).HasColumnName("joined_at");

                entity.HasIndex(x => new { x.OrgId, x.UserId }).IsUnique();
                entity.HasIndex(x => x.OrgId);
                entity.HasIndex(x => x.UserId);

                entity.HasOne(x => x.Organization)
                    .WithMany(x => x.Members)
                    .HasForeignKey(x => x.OrgId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.User)
                    .WithMany(x => x.OrganizationMemberships)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Project>(entity =>
            {
                entity.ToTable("projects");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.OrgId).HasColumnName("org_id");
                entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
                entity.Property(x => x.Description).HasColumnName("description");
                entity.Property(x => x.Deadline).HasColumnName("deadline");
                entity.Property(x => x.CreatedAt).HasColumnName("created_at");
                entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

                entity.HasIndex(x => x.OrgId);

                entity.HasOne(x => x.Organization)
                    .WithMany()
                    .HasForeignKey(x => x.OrgId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProjectMember>(entity =>
            {
                entity.ToTable("project_members");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.ProjectId).HasColumnName("project_id");
                entity.Property(x => x.UserId).HasColumnName("user_id");
                entity.Property(x => x.Role).HasColumnName("role").HasMaxLength(20).HasDefaultValue("Member");
                entity.Property(x => x.JoinedAt).HasColumnName("joined_at");

                entity.HasIndex(x => new { x.ProjectId, x.UserId }).IsUnique();

                entity.HasOne(x => x.Project)
                    .WithMany(x => x.Members)
                    .HasForeignKey(x => x.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProjectTask>(entity =>
            {
                entity.ToTable("tasks");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.ProjectId).HasColumnName("project_id");
                entity.Property(x => x.Title).HasColumnName("title").HasMaxLength(300);
                entity.Property(x => x.Description).HasColumnName("description");
                entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("todo");
                entity.Property(x => x.Priority).HasColumnName("priority").HasMaxLength(20).HasDefaultValue("medium");
                entity.Property(x => x.AssigneeId).HasColumnName("assignee_id");
                entity.Property(x => x.StartDate).HasColumnName("start_date");
                entity.Property(x => x.EndDate).HasColumnName("end_date");
                entity.Property(x => x.Position).HasColumnName("position");
                entity.Property(x => x.CreatedAt).HasColumnName("created_at");
                entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

                entity.HasIndex(x => new { x.ProjectId, x.Status });
                entity.HasIndex(x => x.AssigneeId);

                entity.HasOne(x => x.Project)
                    .WithMany(x => x.Tasks)
                    .HasForeignKey(x => x.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Assignee)
                    .WithMany()
                    .HasForeignKey(x => x.AssigneeId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<TaskComment>(entity =>
            {
                entity.ToTable("task_comments");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.TaskId).HasColumnName("task_id");
                entity.Property(x => x.UserId).HasColumnName("user_id");
                entity.Property(x => x.Content).HasColumnName("content");
                entity.Property(x => x.CreatedAt).HasColumnName("created_at");

                entity.HasIndex(x => x.TaskId);

                entity.HasOne(x => x.Task)
                    .WithMany(x => x.Comments)
                    .HasForeignKey(x => x.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Workspace>(entity =>
            {
                entity.ToTable("workspaces");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
                entity.Property(x => x.OwnerId).HasColumnName("owner_id");
                entity.Property(x => x.OrgId).HasColumnName("org_id");
                entity.Property(x => x.CreatedAt).HasColumnName("created_at");

                entity.HasOne(x => x.Owner)
                    .WithMany()
                    .HasForeignKey(x => x.OwnerId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(x => x.Organization)
                    .WithMany()
                    .HasForeignKey(x => x.OrgId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<WorkspaceMember>(entity =>
            {
                entity.ToTable("workspace_members");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
                entity.Property(x => x.UserId).HasColumnName("user_id");
                entity.Property(x => x.Role).HasColumnName("role").HasMaxLength(20).HasDefaultValue("member");
                entity.Property(x => x.JoinedAt).HasColumnName("joined_at");

                entity.HasIndex(x => new { x.WorkspaceId, x.UserId }).IsUnique();

                entity.HasOne(x => x.Workspace)
                    .WithMany(x => x.Members)
                    .HasForeignKey(x => x.WorkspaceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("notifications");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.UserId).HasColumnName("user_id");
                entity.Property(x => x.Type).HasColumnName("type").HasMaxLength(20).HasDefaultValue("info");
                entity.Property(x => x.Message).HasColumnName("message");
                entity.Property(x => x.IsRead).HasColumnName("is_read").HasDefaultValue(false);
                entity.Property(x => x.CreatedAt).HasColumnName("created_at");

                entity.HasIndex(x => new { x.UserId, x.IsRead }).HasFilter("is_read = false");

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

