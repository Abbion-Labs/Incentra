using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VariableCompensation.Infrastructure.Migrations;

/// <inheritdoc />
public partial class RefactorEmployeeSalaryToPointsAndSalaryPerPoint : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM employee_salaries;");

        migrationBuilder.DropColumn(
            name: "EncryptedAmount",
            table: "employee_salaries");

        migrationBuilder.AddColumn<byte[]>(
            name: "EncryptedSalaryPerPoint",
            table: "employee_salaries",
            type: "bytea",
            nullable: false,
            defaultValue: Array.Empty<byte>());

        migrationBuilder.AddColumn<int>(
            name: "Points",
            table: "employee_salaries",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<bool>(
            name: "AllowNegativeVariable",
            table: "variable_compensation_parameters",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AllowNegativeVariable",
            table: "variable_compensation_parameters");

        migrationBuilder.DropColumn(
            name: "EncryptedSalaryPerPoint",
            table: "employee_salaries");

        migrationBuilder.DropColumn(
            name: "Points",
            table: "employee_salaries");

        migrationBuilder.AddColumn<byte[]>(
            name: "EncryptedAmount",
            table: "employee_salaries",
            type: "bytea",
            nullable: false,
            defaultValue: Array.Empty<byte>());
    }
}
