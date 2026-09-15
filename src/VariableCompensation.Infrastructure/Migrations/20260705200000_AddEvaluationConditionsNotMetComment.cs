using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VariableCompensation.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddEvaluationConditionsNotMetComment : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ConditionsNotMetComment",
            table: "evaluations",
            type: "text",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE evaluations
            SET "ConditionsNotMetComment" = "EvaluatorComment"
            WHERE NOT "ConditionsFulfilled"
              AND "EvaluatorComment" IS NOT NULL
              AND TRIM("EvaluatorComment") <> '';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ConditionsNotMetComment",
            table: "evaluations");
    }
}
