using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VariableCompensation.Infrastructure.Migrations
{
    /// <summary>
    /// Data only. An evaluation sent again after a return for revision kept the return comment as its controller
    /// comment, and the review screen offered it as the comment of the approval. Evaluations waiting for review drop
    /// it now, as sending does from now on; the text stays in RejectionReason and in the status history.
    /// </summary>
    public partial class ClearControllerCommentOfResubmittedEvaluations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE evaluations
                SET "ControllerComment" = NULL
                WHERE "Status" IN ('Submitted', 'UnderReview')
                  AND "ControllerComment" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to restore: the comments are still in RejectionReason and in the status history.
        }
    }
}
