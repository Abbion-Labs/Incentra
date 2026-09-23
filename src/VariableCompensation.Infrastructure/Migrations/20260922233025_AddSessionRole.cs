using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VariableCompensation.Infrastructure.Migrations
{
    /// <summary>
    /// The role a session works in, the role an account last chose, and the role a status change was made in.
    /// Existing rows keep null: a session from before this gets its role on the next refresh.
    /// </summary>
    public partial class AddSessionRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastActiveRoleCode",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActiveRoleCode",
                table: "refresh_tokens",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChangedByRoleCode",
                table: "evaluation_status_history",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastActiveRoleCode",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ActiveRoleCode",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "ChangedByRoleCode",
                table: "evaluation_status_history");
        }
    }
}
