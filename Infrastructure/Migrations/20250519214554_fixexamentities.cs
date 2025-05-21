using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fixexamentities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamGrades");

            migrationBuilder.DropColumn(
                name: "MaxPoints",
                table: "Exams");

            migrationBuilder.AlterColumn<int>(
                name: "LessonId",
                table: "Grades",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "DayIndex",
                table: "Grades",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExamId",
                table: "Grades",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Grades_ExamId",
                table: "Grades",
                column: "ExamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_Exams_ExamId",
                table: "Grades",
                column: "ExamId",
                principalTable: "Exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Grades_Exams_ExamId",
                table: "Grades");

            migrationBuilder.DropIndex(
                name: "IX_Grades_ExamId",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "DayIndex",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "ExamId",
                table: "Grades");

            migrationBuilder.AlterColumn<int>(
                name: "LessonId",
                table: "Grades",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxPoints",
                table: "Exams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ExamGrades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExamId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    BonusPoint = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    HasPassed = table.Column<bool>(type: "boolean", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamGrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamGrades_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamGrades_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamGrades_ExamId",
                table: "ExamGrades",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamGrades_StudentId",
                table: "ExamGrades",
                column: "StudentId");
        }
    }
}
