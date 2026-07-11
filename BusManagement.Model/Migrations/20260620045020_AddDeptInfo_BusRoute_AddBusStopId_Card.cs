using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusManagement.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddDeptInfo_BusRoute_AddBusStopId_Card : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CHR_ACTION",
                table: "TM_MENU",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CHR_CONTROLLER",
                table: "TM_MENU",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CHR_ICON",
                table: "TM_MENU",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_PARENT_ID",
                table: "TM_MENU",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_SORT_ORDER",
                table: "TM_MENU",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CHR_BUS_CODE",
                table: "TM_CARD",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_BUS_STOP_ID",
                table: "TM_CARD",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CHR_CODE_DEPT",
                table: "TM_BUS_ROUTE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CHR_CODE_SEC",
                table: "TM_BUS_ROUTE",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CHR_ACTION",
                table: "TM_MENU");

            migrationBuilder.DropColumn(
                name: "CHR_CONTROLLER",
                table: "TM_MENU");

            migrationBuilder.DropColumn(
                name: "CHR_ICON",
                table: "TM_MENU");

            migrationBuilder.DropColumn(
                name: "INT_PARENT_ID",
                table: "TM_MENU");

            migrationBuilder.DropColumn(
                name: "INT_SORT_ORDER",
                table: "TM_MENU");

            migrationBuilder.DropColumn(
                name: "CHR_BUS_CODE",
                table: "TM_CARD");

            migrationBuilder.DropColumn(
                name: "INT_BUS_STOP_ID",
                table: "TM_CARD");

            migrationBuilder.DropColumn(
                name: "CHR_CODE_DEPT",
                table: "TM_BUS_ROUTE");

            migrationBuilder.DropColumn(
                name: "CHR_CODE_SEC",
                table: "TM_BUS_ROUTE");
        }
    }
}
