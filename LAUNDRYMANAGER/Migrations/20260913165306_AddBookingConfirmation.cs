using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LAUNDRYMANAGER.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmationDeadline",
                table: "Machines",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsConfirmed",
                table: "Machines",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmationDeadline",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "IsConfirmed",
                table: "Machines");
        }
    }
}
