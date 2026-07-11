using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusManagement.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddTable_City_Ward_DriverViolation_BusEvaluation_BusRouteAssign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TM_BUS_EVALUATION",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    INT_DRIVER_ID = table.Column<int>(type: "int", nullable: true),
                    INT_BUS_ID = table.Column<int>(type: "int", nullable: true),
                    CHR_BUS_CODE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    INT_RATING = table.Column<int>(type: "int", nullable: true),
                    NVCHR_COMMENT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_EVALUATION_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DTM_EVALUATION_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_BUS_EVALUATION", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_BUS_ROUTE_ASSIGN",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    INT_ROUTE_ID = table.Column<int>(type: "int", nullable: true),
                    INT_BUS_ID = table.Column<int>(type: "int", nullable: true),
                    INT_DRIVER_ID = table.Column<int>(type: "int", nullable: true),
                    DTM_EFFECTIVE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_BUS_ROUTE_ASSIGN", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_CITY",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CHR_CITY_CODE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_CITY_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BIT_IS_ACTIVE = table.Column<bool>(type: "bit", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_CITY", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_DRIVER_VIOLATION",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    INT_DRIVER_ID = table.Column<int>(type: "int", nullable: true),
                    INT_BUS_ID = table.Column<int>(type: "int", nullable: true),
                    CHR_BUS_CODE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_VIOLATION_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_DESCRIPTION = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DTM_VIOLATION_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_DRIVER_VIOLATION", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_WARD",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    INT_CITY_ID = table.Column<int>(type: "int", nullable: true),
                    CHR_WARD_CODE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_WARD_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BIT_IS_ACTIVE = table.Column<bool>(type: "bit", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_WARD", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TM_BUS_EVALUATION");

            migrationBuilder.DropTable(
                name: "TM_BUS_ROUTE_ASSIGN");

            migrationBuilder.DropTable(
                name: "TM_CITY");

            migrationBuilder.DropTable(
                name: "TM_DRIVER_VIOLATION");

            migrationBuilder.DropTable(
                name: "TM_WARD");
        }
    }
}
