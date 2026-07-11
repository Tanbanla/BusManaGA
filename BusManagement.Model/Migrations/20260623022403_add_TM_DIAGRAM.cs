using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusManagement.Model.Migrations
{
    /// <inheritdoc />
    public partial class add_TM_DIAGRAM : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TM_DIAGRAM",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NVCHR_DIAGRAM_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_DIAGRAM_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_LOCATION = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_FILE_PATH = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    INT_VERSION = table.Column<int>(type: "int", nullable: true),
                    BIT_IS_ACTIVE = table.Column<bool>(type: "bit", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_DIAGRAM", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TM_DIAGRAM");
        }
    }
}
