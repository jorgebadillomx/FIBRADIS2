using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixFibraPlusPrologisMismatch : Migration
    {
        private const string PrologisId = "32377b6d-9244-a715-0279-2660cc6b62a5";
        private const string PlusId     = "32418186-9e2c-942b-8f4a-1e61388760a4";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix FundamentalRecords incorrectly assigned to Prologis via manifest title match
            migrationBuilder.Sql($"""
                UPDATE fundamentals."FundamentalRecord" fr
                SET fibra_id = '{PlusId}'
                FROM fundamentals."FundamentalSourceManifest" fsm
                WHERE fsm.last_processed_record_id = fr.id
                  AND fr.fibra_id = '{PrologisId}'
                  AND (LOWER(fsm.source_title) LIKE '%fibra plus%' OR LOWER(fsm.source_title) LIKE '%fplus%');
                """);

            // Fix manifests incorrectly assigned to Prologis
            migrationBuilder.Sql($"""
                UPDATE fundamentals."FundamentalSourceManifest"
                SET fibra_id = '{PlusId}'
                WHERE fibra_id = '{PrologisId}'
                  AND (LOWER(source_title) LIKE '%fibra plus%' OR LOWER(source_title) LIKE '%fplus%');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reversal: move records back to Prologis — safe only if no new Prologis records were added in between
            migrationBuilder.Sql($"""
                UPDATE fundamentals."FundamentalSourceManifest"
                SET fibra_id = '{PrologisId}'
                WHERE fibra_id = '{PlusId}'
                  AND (LOWER(source_title) LIKE '%fibra plus%' OR LOWER(source_title) LIKE '%fplus%');
                """);

            migrationBuilder.Sql($"""
                UPDATE fundamentals."FundamentalRecord" fr
                SET fibra_id = '{PrologisId}'
                FROM fundamentals."FundamentalSourceManifest" fsm
                WHERE fsm.last_processed_record_id = fr.id
                  AND fr.fibra_id = '{PlusId}'
                  AND (LOWER(fsm.source_title) LIKE '%fibra plus%' OR LOWER(fsm.source_title) LIKE '%fplus%');
                """);
        }
    }
}
