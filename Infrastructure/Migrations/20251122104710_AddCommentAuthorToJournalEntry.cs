using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentAuthorToJournalEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CommentAuthorId",
                table: "JournalEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommentAuthorName",
                table: "JournalEntries",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommentAuthorId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "CommentAuthorName",
                table: "JournalEntries");
        }
    }
}
