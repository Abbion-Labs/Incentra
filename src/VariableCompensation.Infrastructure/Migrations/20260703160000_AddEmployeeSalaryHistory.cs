using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VariableCompensation.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddEmployeeSalaryHistory : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_employee_salaries_EmployeeId",
            table: "employee_salaries");

        migrationBuilder.AddColumn<DateOnly>(
            name: "EffectiveTo",
            table: "employee_salaries",
            type: "date",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_employee_salaries_EmployeeId",
            table: "employee_salaries",
            column: "EmployeeId");

        migrationBuilder.CreateIndex(
            name: "IX_employee_salaries_EmployeeId_EffectiveFrom",
            table: "employee_salaries",
            columns: new[] { "EmployeeId", "EffectiveFrom" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_employee_salaries_EmployeeId_EffectiveFrom",
            table: "employee_salaries");

        migrationBuilder.DropIndex(
            name: "IX_employee_salaries_EmployeeId",
            table: "employee_salaries");

        migrationBuilder.DropColumn(
            name: "EffectiveTo",
            table: "employee_salaries");

        migrationBuilder.CreateIndex(
            name: "IX_employee_salaries_EmployeeId",
            table: "employee_salaries",
            column: "EmployeeId",
            unique: true);
    }
}
