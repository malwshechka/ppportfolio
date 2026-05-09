using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication4.Migrations
{
    /// <inheritdoc />
    public partial class AddChatPinAndDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsArchived",
                table: "Conversations",
                newName: "IsPinnedByUser2");

            migrationBuilder.AddColumn<bool>(
                name: "IsPinnedByUser1",
                table: "Conversations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPinnedByUser1",
                table: "Conversations");

            migrationBuilder.RenameColumn(
                name: "IsPinnedByUser2",
                table: "Conversations",
                newName: "IsArchived");
        }
    }
}
