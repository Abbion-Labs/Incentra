using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VariableCompensation.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddDescriptiveRatingRecommendedShare : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "RecommendedShare",
            table: "descriptive_ratings",
            type: "numeric(5,4)",
            precision: 5,
            scale: 4,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.Sql("""
            UPDATE descriptive_ratings SET "RecommendedShare" = 0.05 WHERE "Code" = 'DOES_NOT_MEET';
            UPDATE descriptive_ratings SET "RecommendedShare" = 0.15 WHERE "Code" = 'MEETS';
            UPDATE descriptive_ratings SET "RecommendedShare" = 0.45 WHERE "Code" = 'GOOD';
            UPDATE descriptive_ratings SET "RecommendedShare" = 0.30 WHERE "Code" = 'EXCEEDS';
            UPDATE descriptive_ratings SET "RecommendedShare" = 0.05 WHERE "Code" = 'OUTSTANDING';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "RecommendedShare",
            table: "descriptive_ratings");
    }
}
