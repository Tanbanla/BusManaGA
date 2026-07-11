using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusManagement.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnsToTMCardUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CHR_SEC_COST",
                table: "TM_CARD_USAGE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CHR_TRAN_CODE",
                table: "TM_CARD_USAGE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_NUMBER_MOVE",
                table: "TM_CARD_USAGE",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CHR_SEC_COST",
                table: "TM_CARD_USAGE");

            migrationBuilder.DropColumn(
                name: "CHR_TRAN_CODE",
                table: "TM_CARD_USAGE");

            migrationBuilder.DropColumn(
                name: "INT_NUMBER_MOVE",
                table: "TM_CARD_USAGE");
        }
    }
}
