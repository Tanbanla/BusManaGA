using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusManagement.Model.Migrations
{
    /// <inheritdoc />
    public partial class Update_TM_BUS_REQUEST : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "INT_ROUTE_ID",
                table: "TM_BUS_REQUEST",
                newName: "INT_BUS_ROUTE_ASSIGN_ID");

            migrationBuilder.AddColumn<string>(
                name: "CHR_NOTE",
                table: "TM_BUS_REQUEST",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CHR_NOTE",
                table: "TM_BUS_REQUEST");

            migrationBuilder.RenameColumn(
                name: "INT_BUS_ROUTE_ASSIGN_ID",
                table: "TM_BUS_REQUEST",
                newName: "INT_ROUTE_ID");
        }
    }
}
