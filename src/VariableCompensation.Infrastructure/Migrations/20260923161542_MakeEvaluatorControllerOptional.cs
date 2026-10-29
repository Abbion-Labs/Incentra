using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VariableCompensation.Infrastructure.Migrations
{
    /// <summary>
    /// An evaluator may have no controller: nobody reviews them, and their evaluations are approved on submission.
    /// An evaluator set up as their own controller meant exactly that, so they become one without a controller,
    /// and so do their drafts.
    /// </summary>
    public partial class MakeEvaluatorControllerOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "ControllerEmployeeId",
                table: "evaluator_settings",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.Sql(
                """
                UPDATE evaluator_settings
                SET "ControllerEmployeeId" = NULL
                WHERE "ControllerEmployeeId" = "EmployeeId";

                UPDATE evaluations
                SET "ControllerEmployeeId" = NULL
                WHERE "Status" = 'Draft'
                  AND "ControllerEmployeeId" = "EvaluatorEmployeeId";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "ControllerEmployeeId",
                table: "evaluator_settings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
