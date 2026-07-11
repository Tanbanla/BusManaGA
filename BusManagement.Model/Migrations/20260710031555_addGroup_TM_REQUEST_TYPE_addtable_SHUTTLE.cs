using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusManagement.Model.Migrations
{
    /// <inheritdoc />
    public partial class addGroup_TM_REQUEST_TYPE_addtable_SHUTTLE : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "INT_GROUP",
                table: "TM_REQUEST_TYPE",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TM_SHUTTLE_LOCATION",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    STR_LOCATION_CODE = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    STR_LOCATION_NAME = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    STR_DESCRIPTION = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    STR_BUILDING_CODE = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    STR_FLOOR_CODE = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DEC_X_COORDINATE = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DEC_Y_COORDINATE = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BIT_IS_ACTIVE = table.Column<bool>(type: "bit", nullable: false),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_SHUTTLE_LOCATION", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_SHUTTLE_SCHEDULE",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    STR_ROUTE_NAME = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    INT_PICKUP_LOCATION_ID = table.Column<int>(type: "int", nullable: false),
                    INT_DROPOFF_LOCATION_ID = table.Column<int>(type: "int", nullable: false),
                    TIM_DEPARTURE_TIME = table.Column<TimeSpan>(type: "time", nullable: false),
                    STR_NOTE = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BIT_IS_LAST_TRIP = table.Column<bool>(type: "bit", nullable: false),
                    BIT_IS_ACTIVE = table.Column<bool>(type: "bit", nullable: false),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_SHUTTLE_SCHEDULE", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TM_SHUTTLE_LOCATION");

            migrationBuilder.DropTable(
                name: "TM_SHUTTLE_SCHEDULE");

            migrationBuilder.DropColumn(
                name: "INT_GROUP",
                table: "TM_REQUEST_TYPE");
        }
    }
}
