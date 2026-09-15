using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VariableCompensation.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddCompensationParameterCurrencyAndThreshold : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "AcceptablePerformanceRating",
            table: "variable_compensation_parameters",
            type: "numeric(4,2)",
            precision: 4,
            scale: 2,
            nullable: false,
            defaultValue: 2.5m);

        migrationBuilder.AddColumn<string>(
            name: "Currency",
            table: "variable_compensation_parameters",
            type: "character varying(3)",
            maxLength: 3,
            nullable: false,
            defaultValue: "RSD");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AcceptablePerformanceRating",
            table: "variable_compensation_parameters");

        migrationBuilder.DropColumn(
            name: "Currency",
            table: "variable_compensation_parameters");
    }
}
