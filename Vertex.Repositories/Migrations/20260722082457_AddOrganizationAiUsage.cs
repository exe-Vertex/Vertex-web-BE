using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vertex.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationAiUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ai_quota_period_start",
                table: "organizations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "date_trunc('month', CURRENT_TIMESTAMP)");

            migrationBuilder.AddColumn<int>(
                name: "ai_used",
                table: "organizations",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ai_quota_period_start",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "ai_used",
                table: "organizations");
        }
    }
}
