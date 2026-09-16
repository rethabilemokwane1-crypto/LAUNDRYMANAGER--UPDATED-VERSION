using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LAUNDRYMANAGER.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingToMachines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BookedBy",
                table: "Machines",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBooked",
                table: "Machines",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookedBy",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "IsBooked",
                table: "Machines");
        }
    }
}
