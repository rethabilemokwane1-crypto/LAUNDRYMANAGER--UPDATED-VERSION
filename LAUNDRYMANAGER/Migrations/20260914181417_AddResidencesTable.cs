using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LAUNDRYMANAGER.Migrations
{
    /// <inheritdoc />
    public partial class AddResidencesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Machines_Residence_ResidenceId",
                table: "Machines");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Residence_ResidenceId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_MachineId",
                table: "Bookings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Residence",
                table: "Residence");

            migrationBuilder.RenameTable(
                name: "Residence",
                newName: "Residences");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Residences",
                table: "Residences",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_MachineId_SlotStart_SlotEnd",
                table: "Bookings",
                columns: new[] { "MachineId", "SlotStart", "SlotEnd" });

            migrationBuilder.AddForeignKey(
                name: "FK_Machines_Residences_ResidenceId",
                table: "Machines",
                column: "ResidenceId",
                principalTable: "Residences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Residences_ResidenceId",
                table: "Users",
                column: "ResidenceId",
                principalTable: "Residences",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Machines_Residences_ResidenceId",
                table: "Machines");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Residences_ResidenceId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_MachineId_SlotStart_SlotEnd",
                table: "Bookings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Residences",
                table: "Residences");

            migrationBuilder.RenameTable(
                name: "Residences",
                newName: "Residence");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Residence",
                table: "Residence",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_MachineId",
                table: "Bookings",
                column: "MachineId");

            migrationBuilder.AddForeignKey(
                name: "FK_Machines_Residence_ResidenceId",
                table: "Machines",
                column: "ResidenceId",
                principalTable: "Residence",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Residence_ResidenceId",
                table: "Users",
                column: "ResidenceId",
                principalTable: "Residence",
                principalColumn: "Id");
        }
    }
}
