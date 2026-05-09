using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication4.Migrations
{
    /// <inheritdoc />
    public partial class InitialOnlineDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConversationParticipants_AspNetUsers_UserId",
                table: "ConversationParticipants");

            migrationBuilder.AddForeignKey(
                name: "FK_ConversationParticipants_AspNetUsers_UserId",
                table: "ConversationParticipants",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConversationParticipants_AspNetUsers_UserId",
                table: "ConversationParticipants");

            migrationBuilder.AddForeignKey(
                name: "FK_ConversationParticipants_AspNetUsers_UserId",
                table: "ConversationParticipants",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
