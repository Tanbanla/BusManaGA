using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusManagement.Model.Migrations
{
    /// <inheritdoc />
    public partial class addTurn_BusRouteStop_AddPickup_ReturnField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "INT_ROUTE_ASSIGN_ID",
                table: "TM_EMPLOYEE_ROUTE");

            migrationBuilder.RenameColumn(
                name: "INT_OLD_ROUTE_ASSIGN_ID",
                table: "TM_EMPLOYEE_ROUTE_HISTORY",
                newName: "INT_OLD_ROUTE_ASSIGN_RETURN_ID");

            migrationBuilder.RenameColumn(
                name: "INT_NEW_ROUTE_ASSIGN_ID",
                table: "TM_EMPLOYEE_ROUTE_HISTORY",
                newName: "INT_OLD_ROUTE_ASSIGN_PICKUP_ID");

            migrationBuilder.AddColumn<int>(
                name: "INT_NEW_PICKUP_TURN_ID",
                table: "TM_EMPLOYEE_ROUTE_HISTORY",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_NEW_RETURN_TURN_ID",
                table: "TM_EMPLOYEE_ROUTE_HISTORY",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_NEW_ROUTE_ASSIGN_PICKUP_ID",
                table: "TM_EMPLOYEE_ROUTE_HISTORY",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_NEW_ROUTE_ASSIGN_RETURN_ID",
                table: "TM_EMPLOYEE_ROUTE_HISTORY",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_OLD_PICKUP_TURN_ID",
                table: "TM_EMPLOYEE_ROUTE_HISTORY",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_OLD_RETURN_TURN_ID",
                table: "TM_EMPLOYEE_ROUTE_HISTORY",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "INT_RETURN_STOP_ID",
                table: "TM_EMPLOYEE_ROUTE",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "INT_PICKUP_STOP_ID",
                table: "TM_EMPLOYEE_ROUTE",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "INT_PICKUP_TURN",
                table: "TM_EMPLOYEE_ROUTE",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_RETURN_TURN",
                table: "TM_EMPLOYEE_ROUTE",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_ROUTE_ASSIGN_PICKUP_ID",
                table: "TM_EMPLOYEE_ROUTE",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_ROUTE_ASSIGN_RETURN_ID",
                table: "TM_EMPLOYEE_ROUTE",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_DIRECTION",
                table: "TM_BUS_ROUTE_STOP",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_TURN",
                table: "TM_BUS_ROUTE_STOP",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NVCHR_TURN_NAME",
                table: "TM_BUS_ROUTE_STOP",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NVCHR_BUS_ROUTE_WORKING_NAME",
                table: "TM_BUS_ROUTE_ASSIGN",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "INT_NEW_PICKUP_TURN_ID",
                table: "TM_EMPLOYEE_ROUTE_HISTORY");

            migrationBuilder.DropColumn(
                name: "INT_NEW_RETURN_TURN_ID",
                table: "TM_EMPLOYEE_ROUTE_HISTORY");

            migrationBuilder.DropColumn(
                name: "INT_NEW_ROUTE_ASSIGN_PICKUP_ID",
                table: "TM_EMPLOYEE_ROUTE_HISTORY");

            migrationBuilder.DropColumn(
                name: "INT_NEW_ROUTE_ASSIGN_RETURN_ID",
                table: "TM_EMPLOYEE_ROUTE_HISTORY");

            migrationBuilder.DropColumn(
                name: "INT_OLD_PICKUP_TURN_ID",
                table: "TM_EMPLOYEE_ROUTE_HISTORY");

            migrationBuilder.DropColumn(
                name: "INT_OLD_RETURN_TURN_ID",
                table: "TM_EMPLOYEE_ROUTE_HISTORY");

            migrationBuilder.DropColumn(
                name: "INT_PICKUP_TURN",
                table: "TM_EMPLOYEE_ROUTE");

            migrationBuilder.DropColumn(
                name: "INT_RETURN_TURN",
                table: "TM_EMPLOYEE_ROUTE");

            migrationBuilder.DropColumn(
                name: "INT_ROUTE_ASSIGN_PICKUP_ID",
                table: "TM_EMPLOYEE_ROUTE");

            migrationBuilder.DropColumn(
                name: "INT_ROUTE_ASSIGN_RETURN_ID",
                table: "TM_EMPLOYEE_ROUTE");

            migrationBuilder.DropColumn(
                name: "INT_DIRECTION",
                table: "TM_BUS_ROUTE_STOP");

            migrationBuilder.DropColumn(
                name: "INT_TURN",
                table: "TM_BUS_ROUTE_STOP");

            migrationBuilder.DropColumn(
                name: "NVCHR_TURN_NAME",
                table: "TM_BUS_ROUTE_STOP");

            migrationBuilder.DropColumn(
                name: "NVCHR_BUS_ROUTE_WORKING_NAME",
                table: "TM_BUS_ROUTE_ASSIGN");

            migrationBuilder.RenameColumn(
                name: "INT_OLD_ROUTE_ASSIGN_RETURN_ID",
                table: "TM_EMPLOYEE_ROUTE_HISTORY",
                newName: "INT_OLD_ROUTE_ASSIGN_ID");

            migrationBuilder.RenameColumn(
                name: "INT_OLD_ROUTE_ASSIGN_PICKUP_ID",
                table: "TM_EMPLOYEE_ROUTE_HISTORY",
                newName: "INT_NEW_ROUTE_ASSIGN_ID");

            migrationBuilder.AlterColumn<int>(
                name: "INT_RETURN_STOP_ID",
                table: "TM_EMPLOYEE_ROUTE",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "INT_PICKUP_STOP_ID",
                table: "TM_EMPLOYEE_ROUTE",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_ROUTE_ASSIGN_ID",
                table: "TM_EMPLOYEE_ROUTE",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
