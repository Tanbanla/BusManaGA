using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusManagement.Model.Migrations
{
    /// <inheritdoc />
    public partial class addNameTableCardUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NVCHR_EMPLOYEE_NAME",
                table: "TM_CARD_USAGE",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NVCHR_EMPLOYEE_NAME",
                table: "TM_CARD_USAGE");
        }
    }
}
