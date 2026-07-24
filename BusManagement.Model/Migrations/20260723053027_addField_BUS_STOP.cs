using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusManagement.Model.Migrations
{
    /// <inheritdoc />
    public partial class addField_BUS_STOP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "FLT_DISTANCE_BIVN_TO_RETURN",
                table: "TM_BUS_STOP",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NVCHR_STOP_RETURN_ADDRESS",
                table: "TM_BUS_STOP",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FLT_DISTANCE_BIVN_TO_RETURN",
                table: "TM_BUS_STOP");

            migrationBuilder.DropColumn(
                name: "NVCHR_STOP_RETURN_ADDRESS",
                table: "TM_BUS_STOP");
        }
    }
}
