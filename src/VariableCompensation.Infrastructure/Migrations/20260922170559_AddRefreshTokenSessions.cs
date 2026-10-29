using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VariableCompensation.Infrastructure.Migrations
{
    /// <summary>
    /// Groups refresh tokens into sessions, one per sign-in, and indexes the token hash every refresh looks up.
    /// Existing tokens are deleted instead of being given a session: they would all share the empty one, so
    /// signing out one of them would sign out every one of them. Nobody loses anything that was still usable,
    /// since access tokens issued before this change carry no session and are rejected anyway, and the frontend
    /// no longer reads the tokens it kept in localStorage. Everyone signs in once more.
    /// </summary>
    public partial class AddRefreshTokenSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM refresh_tokens;");

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                table: "refresh_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_SessionId",
                table: "refresh_tokens",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TokenHash",
                table: "refresh_tokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_SessionId",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_TokenHash",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "refresh_tokens");
        }
    }
}
