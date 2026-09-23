using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VariableCompensation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEditVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_employee_salaries_EmployeeId",
                table: "employee_salaries");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "variable_compensation_parameters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "organization_units",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "job_positions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "evaluator_settings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "employees",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "employee_salaries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "education_levels",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "descriptive_ratings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Should an employee already have more than one salary in force, all but the latest are closed the day
            // before the next one starts, so the index below can be created.
            migrationBuilder.Sql(
                """
                UPDATE employee_salaries s
                SET "EffectiveTo" = (
                    SELECT MIN(n."EffectiveFrom")
                    FROM employee_salaries n
                    WHERE n."EmployeeId" = s."EmployeeId"
                      AND n."EffectiveTo" IS NULL
                      AND (n."EffectiveFrom" > s."EffectiveFrom"
                           OR (n."EffectiveFrom" = s."EffectiveFrom" AND n."Id" > s."Id"))) - 1
                WHERE s."EffectiveTo" IS NULL
                  AND EXISTS (
                    SELECT 1
                    FROM employee_salaries n
                    WHERE n."EmployeeId" = s."EmployeeId"
                      AND n."EffectiveTo" IS NULL
                      AND (n."EffectiveFrom" > s."EffectiveFrom"
                           OR (n."EffectiveFrom" = s."EffectiveFrom" AND n."Id" > s."Id")));
                """);

            migrationBuilder.CreateIndex(
                name: "IX_employee_salaries_EmployeeId_current",
                table: "employee_salaries",
                column: "EmployeeId",
                unique: true,
                filter: "\"EffectiveTo\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_employee_salaries_EmployeeId_current",
                table: "employee_salaries");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "variable_compensation_parameters");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "organization_units");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "job_positions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "evaluator_settings");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "employee_salaries");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "education_levels");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "descriptive_ratings");

            migrationBuilder.CreateIndex(
                name: "IX_employee_salaries_EmployeeId",
                table: "employee_salaries",
                column: "EmployeeId");
        }
    }
}
