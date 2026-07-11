using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusManagement.Model.Migrations
{
    /// <inheritdoc />
    public partial class Add4Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DTM_EFFECTIVE",
                table: "TM_BUS_ROUTE_ASSIGN",
                newName: "DTM_EFFECTIVE_TO");

            migrationBuilder.AddColumn<string>(
                name: "NVCHR_SUGGESTED_ADDRESS",
                table: "TM_EMPLOYEE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NVCHR_SUGGESTED_PICKUP_FROM_BIVN",
                table: "TM_EMPLOYEE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BIT_IS_ACTIVE",
                table: "TM_BUS_ROUTE_ASSIGN",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DTM_EFFECTIVE_FROM",
                table: "TM_BUS_ROUTE_ASSIGN",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "INT_CURRENT_USER",
                table: "TM_BUS_ROUTE_ASSIGN",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TM_BUS_REQUEST_FAILED",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CHR_EMPLOYEE_ID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    INT_ROUTE_ASSIGN_ID = table.Column<int>(type: "int", nullable: false),
                    INT_BUS_STOP_ID = table.Column<int>(type: "int", nullable: false),
                    DTM_START_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DTM_CANCEL_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    INT_REQUEST_TYPE = table.Column<int>(type: "int", nullable: false),
                    INT_STATUS = table.Column<int>(type: "int", nullable: false),
                    CHR_QLSC_APPROVE = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CHR_GA_CONFIRM = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CHR_REASON_FAILED = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DTM_REQUEST_DATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_BUS_REQUEST_FAILED", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_CARD_PRINT_QUEUE",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CHR_EMPLOYEE_ID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    INT_EMPLOYEE_ROUTE_ID = table.Column<int>(type: "int", nullable: false),
                    INT_BUS_REQUEST_ID = table.Column<int>(type: "int", nullable: false),
                    INT_CARD_ID = table.Column<int>(type: "int", nullable: true),
                    INT_PRINT_TYPE = table.Column<int>(type: "int", nullable: false),
                    INT_STATUS = table.Column<int>(type: "int", nullable: false),
                    DTM_REQUEST_DATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DTM_PRINT_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CHR_PRINT_BY = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DTM_RECEIVE_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CHR_RECEIVE_BY = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NVCHR_REASON = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_CARD_PRINT_QUEUE", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_EMPLOYEE_ROUTE",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    INT_EMPLOYEE_ID = table.Column<int>(type: "int", nullable: false),
                    CHR_EMPLOYEE_ID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    INT_ROUTE_ASSIGN_ID = table.Column<int>(type: "int", nullable: false),
                    INT_PICKUP_STOP_ID = table.Column<int>(type: "int", nullable: false),
                    INT_RETURN_STOP_ID = table.Column<int>(type: "int", nullable: false),
                    DTM_EFFECTIVE_FROM = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DTM_EFFECTIVE_TO = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BIT_IS_ACTIVE = table.Column<bool>(type: "bit", nullable: false),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_EMPLOYEE_ROUTE", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_EMPLOYEE_ROUTE_HISTORY",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    INT_EMPLOYEE_ID = table.Column<int>(type: "int", nullable: false),
                    CHR_EMPLOYEE_ID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    INT_REQUEST_ID = table.Column<int>(type: "int", nullable: false),
                    INT_OLD_ROUTE_ASSIGN_ID = table.Column<int>(type: "int", nullable: true),
                    INT_NEW_ROUTE_ASSIGN_ID = table.Column<int>(type: "int", nullable: true),
                    INT_OLD_PICKUP_STOP_ID = table.Column<int>(type: "int", nullable: true),
                    INT_NEW_PICKUP_STOP_ID = table.Column<int>(type: "int", nullable: true),
                    INT_OLD_RETURN_STOP_ID = table.Column<int>(type: "int", nullable: true),
                    INT_NEW_RETURN_STOP_ID = table.Column<int>(type: "int", nullable: true),
                    DTM_NEW_EFFECTIVE_FROM = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NVCHR_ACTION_TYPE = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_EMPLOYEE_ROUTE_HISTORY", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TM_BUS_REQUEST_FAILED");

            migrationBuilder.DropTable(
                name: "TM_CARD_PRINT_QUEUE");

            migrationBuilder.DropTable(
                name: "TM_EMPLOYEE_ROUTE");

            migrationBuilder.DropTable(
                name: "TM_EMPLOYEE_ROUTE_HISTORY");

            migrationBuilder.DropColumn(
                name: "NVCHR_SUGGESTED_ADDRESS",
                table: "TM_EMPLOYEE");

            migrationBuilder.DropColumn(
                name: "NVCHR_SUGGESTED_PICKUP_FROM_BIVN",
                table: "TM_EMPLOYEE");

            migrationBuilder.DropColumn(
                name: "BIT_IS_ACTIVE",
                table: "TM_BUS_ROUTE_ASSIGN");

            migrationBuilder.DropColumn(
                name: "DTM_EFFECTIVE_FROM",
                table: "TM_BUS_ROUTE_ASSIGN");

            migrationBuilder.DropColumn(
                name: "INT_CURRENT_USER",
                table: "TM_BUS_ROUTE_ASSIGN");

            migrationBuilder.RenameColumn(
                name: "DTM_EFFECTIVE_TO",
                table: "TM_BUS_ROUTE_ASSIGN",
                newName: "DTM_EFFECTIVE");
        }
    }
}
