using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LAUNDRYMANAGER.Migrations
{
    /// <inheritdoc />
    public partial class AddResidenceToMachines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResidenceId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ResidenceId",
                table: "Machines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Residence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Residence", x => x.Id);
                });

            // Existing machines received ResidenceId = 0 above. Create a starter
            // residence and assign all existing machines to it before the foreign key
            // is created, otherwise SQL Server rejects ResidenceId = 0.
            migrationBuilder.InsertData(
                table: "Residence",
                columns: new[] { "Name", "Address" },
                values: new object[] { "Brandon Mansions", "Address not yet provided" });

            migrationBuilder.Sql(
                "UPDATE [Machines] SET [ResidenceId] = (SELECT TOP (1) [Id] FROM [Residence] WHERE [Name] = N'Brandon Mansions' ORDER BY [Id])");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ResidenceId",
                table: "Users",
                column: "ResidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Machines_ResidenceId",
                table: "Machines",
                column: "ResidenceId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Machines_Residence_ResidenceId",
                table: "Machines");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Residence_ResidenceId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Residence");

            migrationBuilder.DropIndex(
                name: "IX_Users_ResidenceId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Machines_ResidenceId",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "ResidenceId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResidenceId",
                table: "Machines");
        }
    }
}
