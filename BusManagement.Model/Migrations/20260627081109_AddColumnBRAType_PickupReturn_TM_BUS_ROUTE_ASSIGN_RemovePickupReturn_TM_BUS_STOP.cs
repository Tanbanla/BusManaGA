using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusManagement.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnBRAType_PickupReturn_TM_BUS_ROUTE_ASSIGN_RemovePickupReturn_TM_BUS_STOP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NVCHR_STOP_PICKUP_ADDRESS",
                table: "TM_BUS_STOP");

            migrationBuilder.DropColumn(
                name: "NVCHR_STOP_PICKUP_NAME",
                table: "TM_BUS_STOP");

            migrationBuilder.RenameColumn(
                name: "NVCHR_STOP_RETURN_NAME",
                table: "TM_BUS_STOP",
                newName: "NVCHR_STOP_NAME");

            migrationBuilder.RenameColumn(
                name: "NVCHR_STOP_RETURN_ADDRESS",
                table: "TM_BUS_STOP",
                newName: "NVCHR_STOP_ADDRESS");

            migrationBuilder.RenameColumn(
                name: "INT_ROUTE_ID",
                table: "TM_BUS_ROUTE_ASSIGN",
                newName: "INT_ROUTE_RETURN_ID");

            migrationBuilder.AddColumn<string>(
                name: "CHR_BRA_TYPE",
                table: "TM_BUS_ROUTE_ASSIGN",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_ROUTE_PICKUP_ID",
                table: "TM_BUS_ROUTE_ASSIGN",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CHR_BRA_TYPE",
                table: "TM_BUS_ROUTE_ASSIGN");

            migrationBuilder.DropColumn(
                name: "INT_ROUTE_PICKUP_ID",
                table: "TM_BUS_ROUTE_ASSIGN");

            migrationBuilder.RenameColumn(
                name: "NVCHR_STOP_NAME",
                table: "TM_BUS_STOP",
                newName: "NVCHR_STOP_RETURN_NAME");

            migrationBuilder.RenameColumn(
                name: "NVCHR_STOP_ADDRESS",
                table: "TM_BUS_STOP",
                newName: "NVCHR_STOP_RETURN_ADDRESS");

            migrationBuilder.RenameColumn(
                name: "INT_ROUTE_RETURN_ID",
                table: "TM_BUS_ROUTE_ASSIGN",
                newName: "INT_ROUTE_ID");

            migrationBuilder.AddColumn<string>(
                name: "NVCHR_STOP_PICKUP_ADDRESS",
                table: "TM_BUS_STOP",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NVCHR_STOP_PICKUP_NAME",
                table: "TM_BUS_STOP",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
