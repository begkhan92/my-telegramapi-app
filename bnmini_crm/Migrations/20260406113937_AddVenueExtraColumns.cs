using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bnmini_crm.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueExtraColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Venues",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryFee",
                table: "Venues",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Venues",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ManagerPassword",
                table: "Venues",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "DeliveryFee",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "ManagerPassword",
                table: "Venues");
        }
    }
}
