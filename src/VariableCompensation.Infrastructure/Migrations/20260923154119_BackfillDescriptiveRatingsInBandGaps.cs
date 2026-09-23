using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VariableCompensation.Infrastructure.Migrations
{
    /// <summary>
    /// Data only. Descriptive ratings used to match an average on both ends of their band, so an average between
    /// one band's maximum and the next one's minimum (3.4983 between 3.49 and 3.50, say) got no rating at all.
    /// Such evaluations now get the band whose minimum they have reached, the same rule scoring uses from now on.
    /// </summary>
    public partial class BackfillDescriptiveRatingsInBandGaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE evaluations e
                SET "DescriptiveRatingId" = (
                    SELECT d."Id"
                    FROM descriptive_ratings d
                    WHERE d."IsActive"
                      AND d."MinAverage" IS NOT NULL
                      AND d."MinAverage" <= e."OverallAverage"
                    ORDER BY d."MinAverage" DESC
                    LIMIT 1)
                WHERE e."OverallAverage" IS NOT NULL
                  AND e."DescriptiveRatingId" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Leaving the ratings in place: there is no telling which of them this migration filled in.
        }
    }
}
