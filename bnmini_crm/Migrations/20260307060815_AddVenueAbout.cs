using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bnmini_crm.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueAbout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "About",
                table: "Venues",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "About",
                table: "Venues");
        }
    }
}
