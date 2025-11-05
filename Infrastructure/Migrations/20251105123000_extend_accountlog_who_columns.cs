using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class extend_accountlog_who_columns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PerformedByUserId",
                table: "AccountLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PerformedByName",
                table: "AccountLogs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountLogs_PerformedByUserId",
                table: "AccountLogs",
                column: "PerformedByUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccountLogs_PerformedByUserId",
                table: "AccountLogs");

            migrationBuilder.DropColumn(
                name: "PerformedByUserId",
                table: "AccountLogs");

            migrationBuilder.DropColumn(
                name: "PerformedByName",
                table: "AccountLogs");
        }
    }
}


