using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VariableCompensation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropMeasureDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_evaluation_measures_measure_type_descriptions_MeasureDescri~",
                table: "evaluation_measures");

            migrationBuilder.DropTable(
                name: "measure_type_descriptions");

            migrationBuilder.DropIndex(
                name: "IX_evaluation_measures_MeasureDescriptionId",
                table: "evaluation_measures");

            migrationBuilder.DropColumn(
                name: "CustomDescription",
                table: "evaluation_measures");

            migrationBuilder.DropColumn(
                name: "MeasureDescriptionId",
                table: "evaluation_measures");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomDescription",
                table: "evaluation_measures",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MeasureDescriptionId",
                table: "evaluation_measures",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "measure_type_descriptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MeasureTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_measure_type_descriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_measure_type_descriptions_measure_types_MeasureTypeId",
                        column: x => x.MeasureTypeId,
                        principalTable: "measure_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_measures_MeasureDescriptionId",
                table: "evaluation_measures",
                column: "MeasureDescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_measure_type_descriptions_MeasureTypeId",
                table: "measure_type_descriptions",
                column: "MeasureTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_evaluation_measures_measure_type_descriptions_MeasureDescri~",
                table: "evaluation_measures",
                column: "MeasureDescriptionId",
                principalTable: "measure_type_descriptions",
                principalColumn: "Id");
        }
    }
}
