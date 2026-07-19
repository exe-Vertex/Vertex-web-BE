using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vertex.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddOAuthFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "auth_provider",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "local");

            migrationBuilder.AddColumn<string>(
                name: "external_id",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "auth_provider", table: "users");
            migrationBuilder.DropColumn(name: "external_id", table: "users");
        }
    }
}
