using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VariableCompensation.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddEmployeeAvatarUrl : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AvatarUrl",
            table: "employees",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AvatarUrl",
            table: "employees");
    }
}
