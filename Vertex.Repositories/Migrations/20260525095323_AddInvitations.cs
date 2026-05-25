using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vertex.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_project_files_users_uploaded_by",
                table: "project_files");

            migrationBuilder.AlterColumn<string>(
                name: "storage_path",
                table: "project_files",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.CreateTable(
                name: "invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    target_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invitations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "project_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_project_links_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_links_users_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "task_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    size = table.Column<long>(type: "bigint", nullable: true),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_attachments", x => x.id);
                    table.ForeignKey(
                        name: "FK_task_attachments_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_task_attachments_users_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_invitations_email_target_id_status",
                table: "invitations",
                columns: new[] { "email", "target_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_invitations_token",
                table: "invitations",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_links_project_id",
                table: "project_links",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_links_uploaded_by",
                table: "project_links",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "IX_task_attachments_task_id",
                table: "task_attachments",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_task_attachments_uploaded_by",
                table: "task_attachments",
                column: "uploaded_by");

            migrationBuilder.AddForeignKey(
                name: "FK_project_files_users_uploaded_by",
                table: "project_files",
                column: "uploaded_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_project_files_users_uploaded_by",
                table: "project_files");

            migrationBuilder.DropTable(
                name: "invitations");

            migrationBuilder.DropTable(
                name: "project_links");

            migrationBuilder.DropTable(
                name: "task_attachments");

            migrationBuilder.AlterColumn<string>(
                name: "storage_path",
                table: "project_files",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddForeignKey(
                name: "FK_project_files_users_uploaded_by",
                table: "project_files",
                column: "uploaded_by",
                principalTable: "users",
                principalColumn: "id");
        }
    }
}
