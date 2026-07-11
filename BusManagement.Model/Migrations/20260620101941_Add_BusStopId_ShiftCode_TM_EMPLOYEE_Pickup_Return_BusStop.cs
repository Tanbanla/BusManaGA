using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusManagement.Model.Migrations
{
    /// <inheritdoc />
    public partial class Add_BusStopId_ShiftCode_TM_EMPLOYEE_Pickup_Return_BusStop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NVCHR_STOP_NAME",
                table: "TM_BUS_STOP",
                newName: "NVCHR_STOP_RETURN_NAME");

            migrationBuilder.RenameColumn(
                name: "NVCHR_STOP_ADDRESS",
                table: "TM_BUS_STOP",
                newName: "NVCHR_STOP_RETURN_ADDRESS");

            migrationBuilder.AddColumn<bool>(
                name: "BIT_IS_USE_BUS",
                table: "TM_EMPLOYEE",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CHR_SHIFT_CODE",
                table: "TM_EMPLOYEE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_BUS_STOP_ID",
                table: "TM_EMPLOYEE",
                type: "int",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BIT_IS_USE_BUS",
                table: "TM_EMPLOYEE");

            migrationBuilder.DropColumn(
                name: "CHR_SHIFT_CODE",
                table: "TM_EMPLOYEE");

            migrationBuilder.DropColumn(
                name: "INT_BUS_STOP_ID",
                table: "TM_EMPLOYEE");

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
        }
    }
}
