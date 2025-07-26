using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fixlessontable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoGenerateLessons",
                table: "Groups",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LessonDays",
                table: "Groups",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "LessonEndTime",
                table: "Groups",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "LessonStartTime",
                table: "Groups",
                type: "time",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoGenerateLessons",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "LessonDays",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "LessonEndTime",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "LessonStartTime",
                table: "Groups");
        }
    }
}
