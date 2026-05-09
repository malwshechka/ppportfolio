using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication4.Migrations
{
    /// <inheritdoc />
    public partial class AddChatAndSocialFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Messages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsEdited",
                table: "GroupMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeletedByUser1",
                table: "Conversations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeletedByUser2",
                table: "Conversations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinnedByUser1",
                table: "Conversations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinnedByUser2",
                table: "Conversations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IsEdited",
                table: "GroupMessages");

            migrationBuilder.DropColumn(
                name: "IsDeletedByUser1",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "IsDeletedByUser2",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "IsPinnedByUser1",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "IsPinnedByUser2",
                table: "Conversations");
        }
    }
}
