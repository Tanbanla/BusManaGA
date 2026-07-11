using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusManagement.Model.Migrations
{
    /// <inheritdoc />
    public partial class update_TM_EMPLOYEE_TM_BUS_ROUTE_ASSIGN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "INT_CAPACITY",
                table: "TM_BUS_ROUTE");

            migrationBuilder.AddColumn<int>(
                name: "INT_BUS_ROUTE_ASSIGN_ID",
                table: "TM_EMPLOYEE",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_CAPACITY",
                table: "TM_BUS_ROUTE_ASSIGN",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "INT_BUS_ROUTE_ASSIGN_ID",
                table: "TM_EMPLOYEE");

            migrationBuilder.DropColumn(
                name: "INT_CAPACITY",
                table: "TM_BUS_ROUTE_ASSIGN");

            migrationBuilder.AddColumn<int>(
                name: "INT_CAPACITY",
                table: "TM_BUS_ROUTE",
                type: "int",
                nullable: true);
        }
    }
}
