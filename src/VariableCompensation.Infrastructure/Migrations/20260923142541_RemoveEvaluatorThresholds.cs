using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VariableCompensation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEvaluatorThresholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PercentDoesNotMeet",
                table: "evaluator_settings");

            migrationBuilder.DropColumn(
                name: "PercentExceeds",
                table: "evaluator_settings");

            migrationBuilder.DropColumn(
                name: "PercentGood",
                table: "evaluator_settings");

            migrationBuilder.DropColumn(
                name: "PercentMeets",
                table: "evaluator_settings");

            migrationBuilder.DropColumn(
                name: "ThresholdDoesNotMeet",
                table: "evaluator_settings");

            migrationBuilder.DropColumn(
                name: "ThresholdExceeds",
                table: "evaluator_settings");

            migrationBuilder.DropColumn(
                name: "ThresholdGood",
                table: "evaluator_settings");

            migrationBuilder.DropColumn(
                name: "ThresholdMeets",
                table: "evaluator_settings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PercentDoesNotMeet",
                table: "evaluator_settings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PercentExceeds",
                table: "evaluator_settings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PercentGood",
                table: "evaluator_settings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PercentMeets",
                table: "evaluator_settings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ThresholdDoesNotMeet",
                table: "evaluator_settings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ThresholdExceeds",
                table: "evaluator_settings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ThresholdGood",
                table: "evaluator_settings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ThresholdMeets",
                table: "evaluator_settings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
