using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusManagement.Model.Migrations
{
    /// <inheritdoc />
    public partial class addTaxiID_TM_CARD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "INT_BUS_ID",
                table: "TM_CARD",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_TAXI_ID",
                table: "TM_CARD",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "INT_BUS_ID",
                table: "TM_CARD");

            migrationBuilder.DropColumn(
                name: "INT_TAXI_ID",
                table: "TM_CARD");
        }
    }
}
