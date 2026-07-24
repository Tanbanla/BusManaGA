using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusManagement.Model.Migrations
{
    /// <inheritdoc />
    public partial class addTable_Emp_Route_OT_BUS_OT_ASSIGN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DTM_DATE",
                table: "TM_BUS_OT");

            migrationBuilder.AddColumn<bool>(
                name: "BIT_IS_ACTIVE",
                table: "TM_BUS_OT",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CHR_ROUTE_TYPE",
                table: "TM_BUS_OT",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CHR_SHIFT",
                table: "TM_BUS_OT",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CHR_SHIFT_NOTE",
                table: "TM_BUS_OT",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CHR_SHIFT_TIME",
                table: "TM_BUS_OT",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CHR_TYPE",
                table: "TM_BUS_OT",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_DRIVER_ID",
                table: "TM_BUS_OT",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_OT_TYPE",
                table: "TM_BUS_OT",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NVCHR_BUS_OT_WORKING_NAME",
                table: "TM_BUS_OT",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TM_BUS_OT_ASSIGN",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NVCHR_BUS_OT_WORKING_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    INT_BUS_OT_ID = table.Column<int>(type: "int", nullable: true),
                    INT_OT_TYPE = table.Column<int>(type: "int", nullable: true),
                    CHR_ROUTE_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_SHIFT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_SHIFT_TIME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_SHIFT_NOTE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DTM_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    INT_CAPACITY = table.Column<int>(type: "int", nullable: true),
                    INT_CURRENT_USER = table.Column<int>(type: "int", nullable: true),
                    INT_ROUTE_ID = table.Column<int>(type: "int", nullable: true),
                    INT_BUS_ID = table.Column<int>(type: "int", nullable: true),
                    INT_DRIVER_ID = table.Column<int>(type: "int", nullable: true),
                    BIT_IS_ACTIVE = table.Column<bool>(type: "bit", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_BUS_OT_ASSIGN", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_EMPLOYEE_ROUTE_OT",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    INT_EMPLOYEE_ID = table.Column<int>(type: "int", nullable: false),
                    CHR_EMPLOYEE_ID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    INT_BUS_OT_ASSIGN_ID = table.Column<int>(type: "int", nullable: true),
                    INT_BUS_STOP_ID = table.Column<int>(type: "int", nullable: true),
                    BIT_IS_ACTIVE = table.Column<bool>(type: "bit", nullable: false),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_EMPLOYEE_ROUTE_OT", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TM_BUS_OT_ASSIGN");

            migrationBuilder.DropTable(
                name: "TM_EMPLOYEE_ROUTE_OT");

            migrationBuilder.DropColumn(
                name: "BIT_IS_ACTIVE",
                table: "TM_BUS_OT");

            migrationBuilder.DropColumn(
                name: "CHR_ROUTE_TYPE",
                table: "TM_BUS_OT");

            migrationBuilder.DropColumn(
                name: "CHR_SHIFT",
                table: "TM_BUS_OT");

            migrationBuilder.DropColumn(
                name: "CHR_SHIFT_NOTE",
                table: "TM_BUS_OT");

            migrationBuilder.DropColumn(
                name: "CHR_SHIFT_TIME",
                table: "TM_BUS_OT");

            migrationBuilder.DropColumn(
                name: "CHR_TYPE",
                table: "TM_BUS_OT");

            migrationBuilder.DropColumn(
                name: "INT_DRIVER_ID",
                table: "TM_BUS_OT");

            migrationBuilder.DropColumn(
                name: "INT_OT_TYPE",
                table: "TM_BUS_OT");

            migrationBuilder.DropColumn(
                name: "NVCHR_BUS_OT_WORKING_NAME",
                table: "TM_BUS_OT");

            migrationBuilder.AddColumn<DateTime>(
                name: "DTM_DATE",
                table: "TM_BUS_OT",
                type: "datetime2",
                nullable: true);
        }
    }
}
