using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayrollContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MentorId = table.Column<int>(type: "integer", nullable: true),
                    EmployeeUserId = table.Column<int>(type: "integer", nullable: true),
                    CenterId = table.Column<int>(type: "integer", nullable: false),
                    SalaryType = table.Column<string>(type: "text", nullable: false),
                    FixedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    HourlyRate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StudentPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollContracts_Centers_CenterId",
                        column: x => x.CenterId,
                        principalTable: "Centers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollContracts_Mentors_MentorId",
                        column: x => x.MentorId,
                        principalTable: "Mentors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollContracts_Users_EmployeeUserId",
                        column: x => x.EmployeeUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MentorId = table.Column<int>(type: "integer", nullable: true),
                    EmployeeUserId = table.Column<int>(type: "integer", nullable: true),
                    CenterId = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    FixedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    HourlyAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalHours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    PercentageAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalStudentPayments = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PercentageRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    BonusAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BonusReason = table.Column<string>(type: "text", nullable: true),
                    FineAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FineReason = table.Column<string>(type: "text", nullable: true),
                    AdvanceDeduction = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedByUserId = table.Column<int>(type: "integer", nullable: true),
                    PaidDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentMethod = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollRecords_Centers_CenterId",
                        column: x => x.CenterId,
                        principalTable: "Centers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRecords_Mentors_MentorId",
                        column: x => x.MentorId,
                        principalTable: "Mentors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRecords_Users_EmployeeUserId",
                        column: x => x.EmployeeUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MentorId = table.Column<int>(type: "integer", nullable: true),
                    EmployeeUserId = table.Column<int>(type: "integer", nullable: true),
                    CenterId = table.Column<int>(type: "integer", nullable: false),
                    WorkDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Hours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    GroupId = table.Column<int>(type: "integer", nullable: true),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkLogs_Centers_CenterId",
                        column: x => x.CenterId,
                        principalTable: "Centers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkLogs_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkLogs_Mentors_MentorId",
                        column: x => x.MentorId,
                        principalTable: "Mentors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkLogs_Users_EmployeeUserId",
                        column: x => x.EmployeeUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Advances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MentorId = table.Column<int>(type: "integer", nullable: true),
                    EmployeeUserId = table.Column<int>(type: "integer", nullable: true),
                    CenterId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GivenDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    TargetMonth = table.Column<int>(type: "integer", nullable: false),
                    TargetYear = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PayrollRecordId = table.Column<int>(type: "integer", nullable: true),
                    GivenByUserId = table.Column<int>(type: "integer", nullable: false),
                    GivenByName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Advances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Advances_Centers_CenterId",
                        column: x => x.CenterId,
                        principalTable: "Centers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Advances_Mentors_MentorId",
                        column: x => x.MentorId,
                        principalTable: "Mentors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Advances_PayrollRecords_PayrollRecordId",
                        column: x => x.PayrollRecordId,
                        principalTable: "PayrollRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Advances_Users_EmployeeUserId",
                        column: x => x.EmployeeUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Advances_CenterId",
                table: "Advances",
                column: "CenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Advances_EmployeeUserId",
                table: "Advances",
                column: "EmployeeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Advances_MentorId",
                table: "Advances",
                column: "MentorId");

            migrationBuilder.CreateIndex(
                name: "IX_Advances_PayrollRecordId",
                table: "Advances",
                column: "PayrollRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_Advances_Status",
                table: "Advances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Advances_TargetMonth_TargetYear",
                table: "Advances",
                columns: new[] { "TargetMonth", "TargetYear" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollContracts_CenterId",
                table: "PayrollContracts",
                column: "CenterId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollContracts_EmployeeUserId",
                table: "PayrollContracts",
                column: "EmployeeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollContracts_MentorId",
                table: "PayrollContracts",
                column: "MentorId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRecords_CenterId",
                table: "PayrollRecords",
                column: "CenterId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRecords_EmployeeUserId",
                table: "PayrollRecords",
                column: "EmployeeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRecords_MentorId",
                table: "PayrollRecords",
                column: "MentorId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRecords_Month_Year",
                table: "PayrollRecords",
                columns: new[] { "Month", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRecords_Status",
                table: "PayrollRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkLogs_CenterId",
                table: "WorkLogs",
                column: "CenterId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkLogs_EmployeeUserId",
                table: "WorkLogs",
                column: "EmployeeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkLogs_GroupId",
                table: "WorkLogs",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkLogs_MentorId",
                table: "WorkLogs",
                column: "MentorId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkLogs_Month_Year",
                table: "WorkLogs",
                columns: new[] { "Month", "Year" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Advances");

            migrationBuilder.DropTable(
                name: "PayrollContracts");

            migrationBuilder.DropTable(
                name: "WorkLogs");

            migrationBuilder.DropTable(
                name: "PayrollRecords");
        }
    }
}
