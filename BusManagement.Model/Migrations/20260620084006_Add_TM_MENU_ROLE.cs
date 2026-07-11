using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusManagement.Model.Migrations
{
    /// <inheritdoc />
    public partial class Add_TM_MENU_ROLE : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "INT_ROLE_ID",
                table: "TM_MENU");

            migrationBuilder.CreateTable(
                name: "TM_MENU_ROLE",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    INT_MENU_ID = table.Column<int>(type: "int", nullable: false),
                    INT_ROLE_ID = table.Column<int>(type: "int", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_MENU_ROLE", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TM_MENU_ROLE");

            migrationBuilder.AddColumn<int>(
                name: "INT_ROLE_ID",
                table: "TM_MENU",
                type: "int",
                nullable: true);
        }
    }
}
