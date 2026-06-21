using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vertex.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_code = table.Column<long>(type: "bigint", nullable: false),
                    payment_link_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "payos"),
                    plan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    billing_cycle = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "VND"),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    checkout_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    qr_code = table.Column<string>(type: "text", nullable: true),
                    payos_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_transactions_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_payment_transactions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_order_code",
                table: "payment_transactions",
                column: "order_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_org_id_status",
                table: "payment_transactions",
                columns: new[] { "org_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_payment_link_id",
                table: "payment_transactions",
                column: "payment_link_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_user_id",
                table: "payment_transactions",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "payment_transactions");
        }
    }
}
