using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VariableCompensation.Infrastructure.Migrations
{
    /// <summary>
    /// Data only. Granting the EVALUATOR role is now what creates and removes the
    /// evaluator settings, so anyone who already has settings must carry the role.
    /// Without this, editing such a user -- even only their email -- would look
    /// like the role was being taken away and would drop their settings.
    /// </summary>
    public partial class GrantEvaluatorRoleToConfiguredEvaluators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO user_roles ("UserId", "RoleId", "AssignedAt")
                SELECT e."UserId", r."Id", NOW() AT TIME ZONE 'UTC'
                FROM evaluator_settings s
                JOIN employees e ON e."Id" = s."EmployeeId"
                JOIN roles r ON r."Code" = 'EVALUATOR'
                WHERE e."UserId" IS NOT NULL
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Leaving the roles in place: there is no way to tell which of them this
            // migration granted and which an administrator granted afterwards.
        }
    }
}
