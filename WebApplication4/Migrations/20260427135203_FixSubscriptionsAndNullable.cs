using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication4.Migrations
{
    /// <inheritdoc />
    public partial class FixSubscriptionsAndNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReporterId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReportedUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdditionalComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    AdminResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserReports_AspNetUsers_ReportedUserId",
                        column: x => x.ReportedUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserReports_AspNetUsers_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubscriberId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FollowedUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SubscribedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_AspNetUsers_FollowedUserId",
                        column: x => x.FollowedUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_AspNetUsers_SubscriberId",
                        column: x => x.SubscriberId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserReports_ReportedUserId",
                table: "UserReports",
                column: "ReportedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReports_ReporterId_ReportedUserId_ReportedAt",
                table: "UserReports",
                columns: new[] { "ReporterId", "ReportedUserId", "ReportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_FollowedUserId",
                table: "UserSubscriptions",
                column: "FollowedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_SubscriberId_FollowedUserId",
                table: "UserSubscriptions",
                columns: new[] { "SubscriberId", "FollowedUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserReports");

            migrationBuilder.DropTable(
                name: "UserSubscriptions");
        }
    }
}
