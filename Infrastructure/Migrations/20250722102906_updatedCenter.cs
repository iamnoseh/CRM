using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatedCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ManagerName",
                table: "Centers");

            migrationBuilder.AddColumn<int>(
                name: "ManagerId",
                table: "Centers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Centers_ManagerId",
                table: "Centers",
                column: "ManagerId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Centers_AspNetUsers_ManagerId",
                table: "Centers",
                column: "ManagerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Centers_AspNetUsers_ManagerId",
                table: "Centers");

            migrationBuilder.DropIndex(
                name: "IX_Centers_ManagerId",
                table: "Centers");

            migrationBuilder.DropColumn(
                name: "ManagerId",
                table: "Centers");

            migrationBuilder.AddColumn<string>(
                name: "ManagerName",
                table: "Centers",
                type: "text",
                nullable: true);
        }
    }
}
