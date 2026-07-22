using Microsoft.EntityFrameworkCore;
using Vertex.Entities.AI;
using Vertex.Entities.AuditLogs;
using Vertex.Entities.Auth;
using Vertex.Entities.Billing;
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

        // ── Existing ──
        public DbSet<User> Users => Set<User>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
        public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

        // ── New ──
        public DbSet<UserSkill> UserSkills => Set<UserSkill>();
        public DbSet<Workspace> Workspaces => Set<Workspace>();
        public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
        public DbSet<Subtask> Subtasks => Set<Subtask>();
        public DbSet<TaskComment> TaskComments => Set<TaskComment>();
        public DbSet<ProjectFile> ProjectFiles => Set<ProjectFile>();
        public DbSet<ProjectLink> ProjectLinks => Set<ProjectLink>();
        public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();
        public DbSet<AiHistory> AiHistories => Set<AiHistory>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Invitation> Invitations => Set<Invitation>();
        public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ── 1. Users ──────────────────────────────────────
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
                entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
                entity.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
                entity.Property(x => x.AuthProvider).HasColumnName("auth_provider").HasMaxLength(20).HasDefaultValue("local");
                entity.Property(x => x.ExternalId).HasColumnName("external_id").HasMaxLength(255);
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

            // ── 2. Refresh Tokens ─────────────────────────────
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

            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.ToTable("password_reset_tokens");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.UserId).HasColumnName("user_id");
                entity.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(64);
                entity.Property(x => x.ExpiresAt).HasColumnName("expires_at");
                entity.Property(x => x.CreatedAt).HasColumnName("created_at");
                entity.Property(x => x.UsedAt).HasColumnName("used_at");

                entity.HasIndex(x => x.TokenHash).IsUnique();
                entity.HasIndex(x => x.UserId);

                entity.HasOne(x => x.User)
                    .WithMany(x => x.PasswordResetTokens)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            // ── 3. User Skills ────────────────────────────────
            modelBuilder.Entity<UserSkill>(entity =>
            {
                entity.ToTable("user_skills");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.UserId).HasColumnName("user_id");
                entity.Property(x => x.SkillName).HasColumnName("skill_name").HasMaxLength(50);

                entity.HasIndex(x => x.UserId);
                entity.HasIndex(x => new { x.UserId, x.SkillName }).IsUnique();

                entity.HasOne(x => x.User)
                    .WithMany(x => x.Skills)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── 4. Organizations ──────────────────────────────
            modelBuilder.Entity<Organization>(entity =>
            {
                entity.ToTable("organizations");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
                entity.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(100);
                entity.Property(x => x.Plan).HasColumnName("plan").HasMaxLength(20).HasDefaultValue("free");
                entity.Property(x => x.MaxMembers).HasColumnName("max_members");
                entity.Property(x => x.MaxProjects).HasColumnName("max_projects").HasDefaultValue(3);
                entity.Property(x => x.AiQuota).HasColumnName("ai_quota");
                entity.Property(x => x.AiUsed).HasColumnName("ai_used").HasDefaultValue(0);
                entity.Property(x => x.AiQuotaPeriodStart)
                    .HasColumnName("ai_quota_period_start")
                    .HasDefaultValueSql("date_trunc('month', CURRENT_TIMESTAMP)");
                entity.Property(x => x.StorageLimit).HasColumnName("storage_limit");
                entity.Property(x => x.CreatedAt).HasColumnName("created_at");
                entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

                entity.HasIndex(x => x.Slug).IsUnique();
            });

            // ── 5. Organization Members ───────────────────────
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

            // ── 6. Workspaces ─────────────────────────────────
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

            // ── 7. Workspace Members ──────────────────────────
            modelBuilder.Entity<WorkspaceMember>(entity =>
            {
                entity.ToTable("workspace_members");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
                entity.Property(x => x.UserId).HasColumnName("user_id");
                entity.Property(x => x.Role).HasColumnName("role").HasMaxLength(20);
                entity.Property(x => x.JoinedAt).HasColumnName("joined_at");

                entity.HasIndex(x => new { x.WorkspaceId, x.UserId }).IsUnique();

                entity.HasOne(x => x.Workspace)
                    .WithMany(x => x.Members)
                    .HasForeignKey(x => x.WorkspaceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.User)
                    .WithMany(x => x.WorkspaceMemberships)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── 8. Projects ───────────────────────────────────
            modelBuilder.Entity<Project>(entity =>
            {
                entity.ToTable("projects");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.OrgId).HasColumnName("org_id");
                entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
                entity.Property(x => x.Description).HasColumnName("description");
                entity.Property(x => x.Deadline).HasColumnName("deadline").HasColumnType("date");
                entity.Property(x => x.CreatedAt).HasColumnName("created_at");
                entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

                entity.HasIndex(x => x.OrgId);

                entity.HasOne(x => x.Organization)
                    .WithMany()
                    .HasForeignKey(x => x.OrgId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── 9. Project Members ────────────────────────────
            modelBuilder.Entity<ProjectMember>(entity =>
            {
                entity.ToTable("project_members");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.ProjectId).HasColumnName("project_id");
                entity.Property(x => x.UserId).HasColumnName("user_id");
                entity.Property(x => x.Role).HasColumnName("role").HasMaxLength(20);
                entity.Property(x => x.ProjectSkills).HasColumnName("project_skills").HasMaxLength(500);
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

            // ── 10. Tasks ─────────────────────────────────────
            modelBuilder.Entity<ProjectTask>(entity =>
            {
                entity.ToTable("tasks");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.ProjectId).HasColumnName("project_id");
                entity.Property(x => x.Title).HasColumnName("title").HasMaxLength(300);
                entity.Property(x => x.Description).HasColumnName("description");
                entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(30);
                entity.Property(x => x.Priority).HasColumnName("priority").HasMaxLength(10);
                entity.Property(x => x.AssigneeId).HasColumnName("assignee_id");
                entity.Property(x => x.StartDate).HasColumnName("start_date").HasColumnType("date");
                entity.Property(x => x.EndDate).HasColumnName("end_date").HasColumnType("date");
                entity.Property(x => x.Position).HasColumnName("position");
                entity.Property(x => x.SubmissionLink).HasColumnName("submission_link").HasMaxLength(2000);
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

            // ── 11. Subtasks ──────────────────────────────────
            modelBuilder.Entity<Subtask>(entity =>
            {
                entity.ToTable("subtasks");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.TaskId).HasColumnName("task_id");
                entity.Property(x => x.Title).HasColumnName("title").HasMaxLength(300);
                entity.Property(x => x.IsCompleted).HasColumnName("is_completed").HasDefaultValue(false);
                entity.Property(x => x.Position).HasColumnName("position");

                entity.HasIndex(x => x.TaskId);

                entity.HasOne(x => x.Task)
                    .WithMany(x => x.Subtasks)
                    .HasForeignKey(x => x.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── 12. Task Comments ─────────────────────────────
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

            // ── 13. Project Files ─────────────────────────────
            modelBuilder.Entity<ProjectFile>(entity =>
            {
                entity.ToTable("project_files");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.ProjectId).HasColumnName("project_id");
                entity.Property(x => x.UploadedBy).HasColumnName("uploaded_by");
                entity.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(300);
                entity.Property(x => x.FileSize).HasColumnName("file_size");
                entity.Property(x => x.MimeType).HasColumnName("mime_type").HasMaxLength(100);
                entity.Property(x => x.StoragePath).HasColumnName("storage_path").HasMaxLength(500);
                entity.Property(x => x.CreatedAt).HasColumnName("created_at");

                entity.HasIndex(x => x.ProjectId);

                entity.HasOne(x => x.Project)
                    .WithMany(x => x.Files)
                    .HasForeignKey(x => x.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Uploader)
                    .WithMany()
                    .HasForeignKey(x => x.UploadedBy)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // ── 14. AI History ────────────────────────────────
            modelBuilder.Entity<AiHistory>(entity =>
            {
                entity.ToTable("ai_history");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.UserId).HasColumnName("user_id");
                entity.Property(x => x.Prompt).HasColumnName("prompt");
                entity.Property(x => x.PlanSummary).HasColumnName("plan_summary");
                entity.Property(x => x.PlanData).HasColumnName("plan_data").HasColumnType("jsonb");
                entity.Property(x => x.TokensUsed).HasColumnName("tokens_used");
                entity.Property(x => x.CreatedAt).HasColumnName("created_at");

                entity.HasIndex(x => x.UserId);

                entity.HasOne(x => x.User)
                    .WithMany(x => x.AiHistories)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── 15. Notifications ─────────────────────────────
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

                entity.HasIndex(x => new { x.UserId, x.IsRead })
                    .HasFilter("is_read = FALSE");

                entity.HasOne(x => x.User)
                    .WithMany(x => x.Notifications)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── 16. Audit Logs ────────────────────────────────
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("audit_logs");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.AdminId).HasColumnName("admin_id");
                entity.Property(x => x.Action).HasColumnName("action").HasMaxLength(30);
                entity.Property(x => x.TargetUserId).HasColumnName("target_user_id");
                entity.Property(x => x.Detail).HasColumnName("detail");
                entity.Property(x => x.CreatedAt).HasColumnName("created_at");

                entity.HasIndex(x => x.AdminId);
                entity.HasIndex(x => x.CreatedAt).IsDescending();

                entity.HasOne(x => x.Admin)
                    .WithMany()
                    .HasForeignKey(x => x.AdminId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(x => x.TargetUser)
                    .WithMany()
                    .HasForeignKey(x => x.TargetUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ── 17. Project Files ─────────────────────────────
            modelBuilder.Entity<ProjectFile>(entity =>
            {
                entity.ToTable("project_files");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.ProjectId).HasColumnName("project_id");
                entity.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(300);
                entity.Property(x => x.StoragePath).HasColumnName("storage_path").HasMaxLength(1000);
                entity.Property(x => x.FileSize).HasColumnName("file_size");
                entity.Property(x => x.MimeType).HasColumnName("mime_type").HasMaxLength(100);
                entity.Property(x => x.UploadedBy).HasColumnName("uploaded_by");
                entity.Property(x => x.CreatedAt).HasColumnName("created_at");

                entity.HasOne(x => x.Project)
                    .WithMany(x => x.Files)
                    .HasForeignKey(x => x.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Uploader)
                    .WithMany()
                    .HasForeignKey(x => x.UploadedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ── 18. Project Links ─────────────────────────────
            modelBuilder.Entity<ProjectLink>(entity =>
            {
                entity.ToTable("project_links");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.ProjectId).HasColumnName("project_id");
                entity.Property(x => x.Url).HasColumnName("url").HasMaxLength(2000);
                entity.Property(x => x.Title).HasColumnName("title").HasMaxLength(300);
                entity.Property(x => x.UploadedBy).HasColumnName("uploaded_by");
                entity.Property(x => x.CreatedAt).HasColumnName("created_at");

                entity.HasIndex(x => x.ProjectId);

                entity.HasOne(x => x.Project)
                    .WithMany(x => x.Links)
                    .HasForeignKey(x => x.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Uploader)
                    .WithMany()
                    .HasForeignKey(x => x.UploadedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ── 19. Task Attachments ──────────────────────────────────
            modelBuilder.Entity<TaskAttachment>(entity =>
            {
                entity.ToTable("task_attachments");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.TaskId).HasColumnName("task_id");
                entity.Property(x => x.Type).HasColumnName("type").HasMaxLength(20);
                entity.Property(x => x.Url).HasColumnName("url").HasMaxLength(2000);
                entity.Property(x => x.Title).HasColumnName("title").HasMaxLength(300);
                entity.Property(x => x.Size).HasColumnName("size");
                entity.Property(x => x.MimeType).HasColumnName("mime_type").HasMaxLength(100);
                entity.Property(x => x.UploadedBy).HasColumnName("uploaded_by");
                entity.Property(x => x.CreatedAt).HasColumnName("created_at");

                entity.HasIndex(x => x.TaskId);

                entity.HasOne(x => x.Task)
                    .WithMany()
                    .HasForeignKey(x => x.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Uploader)
                    .WithMany()
                    .HasForeignKey(x => x.UploadedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ── 20. Invitations ──────────────────────────────────────
            modelBuilder.Entity<Invitation>(entity =>
            {
                entity.ToTable("invitations");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
                entity.Property(x => x.TargetType).HasColumnName("target_type").HasMaxLength(20);
                entity.Property(x => x.TargetId).HasColumnName("target_id");
                entity.Property(x => x.Role).HasColumnName("role").HasMaxLength(20);
                entity.Property(x => x.Token).HasColumnName("token").HasMaxLength(100);
                entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
                entity.Property(x => x.CreatedBy).HasColumnName("created_by");
                entity.Property(x => x.CreatedAt).HasColumnName("created_at");
                entity.Property(x => x.ExpiredAt).HasColumnName("expired_at");

                entity.HasIndex(x => x.Token).IsUnique();
                entity.HasIndex(x => new { x.Email, x.TargetId, x.Status });
            });

            // 21. Payment Transactions
            modelBuilder.Entity<PaymentTransaction>(entity =>
            {
                entity.ToTable("payment_transactions");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.OrgId).HasColumnName("org_id");
                entity.Property(x => x.UserId).HasColumnName("user_id");
                entity.Property(x => x.OrderCode).HasColumnName("order_code");
                entity.Property(x => x.PaymentLinkId).HasColumnName("payment_link_id").HasMaxLength(100);
                entity.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(30).HasDefaultValue("payos");
                entity.Property(x => x.Plan).HasColumnName("plan").HasMaxLength(20);
                entity.Property(x => x.BillingCycle).HasColumnName("billing_cycle").HasMaxLength(20);
                entity.Property(x => x.Amount).HasColumnName("amount");
                entity.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(10).HasDefaultValue("VND");
                entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
                entity.Property(x => x.CheckoutUrl).HasColumnName("checkout_url").HasMaxLength(1000);
                entity.Property(x => x.QrCode).HasColumnName("qr_code");
                entity.Property(x => x.PayosReference).HasColumnName("payos_reference").HasMaxLength(100);
                entity.Property(x => x.FailureReason).HasColumnName("failure_reason").HasMaxLength(500);
                entity.Property(x => x.PaidAt).HasColumnName("paid_at");
                entity.Property(x => x.ExpiredAt).HasColumnName("expired_at");
                entity.Property(x => x.CreatedAt).HasColumnName("created_at");
                entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

                entity.HasIndex(x => x.OrderCode).IsUnique();
                entity.HasIndex(x => new { x.OrgId, x.Status });
                entity.HasIndex(x => x.PaymentLinkId);

                entity.HasOne(x => x.Organization)
                    .WithMany()
                    .HasForeignKey(x => x.OrgId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
