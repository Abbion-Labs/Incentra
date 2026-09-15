using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VariableCompensation.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddEvaluationTrainingEvaluatorComment : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "EvaluatorComment",
            table: "evaluation_trainings",
            type: "text",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EvaluatorComment",
            table: "evaluation_trainings");
    }
}
