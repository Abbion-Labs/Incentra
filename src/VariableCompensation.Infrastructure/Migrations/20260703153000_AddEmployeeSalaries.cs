using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VariableCompensation.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddEmployeeSalaries : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "employee_salaries",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                EmployeeId = table.Column<long>(type: "bigint", nullable: false),
                EncryptedAmount = table.Column<byte[]>(type: "bytea", nullable: false),
                KeyVersion = table.Column<int>(type: "integer", nullable: false),
                Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_employee_salaries", x => x.Id);
                table.ForeignKey(
                    name: "FK_employee_salaries_employees_EmployeeId",
                    column: x => x.EmployeeId,
                    principalTable: "employees",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_employee_salaries_EmployeeId",
            table: "employee_salaries",
            column: "EmployeeId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "employee_salaries");
    }
}
