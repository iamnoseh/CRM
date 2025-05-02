using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Centers_CenterId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Mentors_Centers_CenterId",
                table: "Mentors");

            migrationBuilder.AlterColumn<int>(
                name: "CenterId",
                table: "Mentors",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CenterId",
                table: "Courses",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Centers_CenterId",
                table: "Courses",
                column: "CenterId",
                principalTable: "Centers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Mentors_Centers_CenterId",
                table: "Mentors",
                column: "CenterId",
                principalTable: "Centers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Centers_CenterId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Mentors_Centers_CenterId",
                table: "Mentors");

            migrationBuilder.AlterColumn<int>(
                name: "CenterId",
                table: "Mentors",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "CenterId",
                table: "Courses",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Centers_CenterId",
                table: "Courses",
                column: "CenterId",
                principalTable: "Centers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Mentors_Centers_CenterId",
                table: "Mentors",
                column: "CenterId",
                principalTable: "Centers",
                principalColumn: "Id");
        }
    }
}
