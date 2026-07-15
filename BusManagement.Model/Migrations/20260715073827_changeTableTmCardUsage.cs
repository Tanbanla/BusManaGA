using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusManagement.Model.Migrations
{
    /// <inheritdoc />
    public partial class changeTableTmCardUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "INT_CARD_ID",
                table: "TM_CARD_USAGE",
                newName: "INT_BORROW_ID");

            migrationBuilder.RenameColumn(
                name: "CHR_CARD_CODE",
                table: "TM_CARD_USAGE",
                newName: "NVCHR_REASON");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NVCHR_REASON",
                table: "TM_CARD_USAGE",
                newName: "CHR_CARD_CODE");

            migrationBuilder.RenameColumn(
                name: "INT_BORROW_ID",
                table: "TM_CARD_USAGE",
                newName: "INT_CARD_ID");
        }
    }
}
