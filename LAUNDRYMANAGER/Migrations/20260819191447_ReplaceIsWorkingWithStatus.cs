using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LAUNDRYMANAGER.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIsWorkingWithStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsWorking",
                table: "Machines");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Machines",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Machines");

            migrationBuilder.AddColumn<bool>(
                name: "IsWorking",
                table: "Machines",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
