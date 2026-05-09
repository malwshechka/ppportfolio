using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication4.Migrations
{
    /// <inheritdoc />
    public partial class AddModerationToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ModeratedAt",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModeratedByUserId",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModerationStatus",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Images",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadedAt",
                table: "Images",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Images",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Images_ProjectId",
                table: "Images",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Images_Projects_ProjectId",
                table: "Images",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Images_Projects_ProjectId",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_Images_ProjectId",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "ModeratedAt",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ModeratedByUserId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ModerationStatus",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "UploadedAt",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "Images");
        }
    }
}
