using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bn_Food_Ordering_Bot.Migrations
{
    /// <inheritdoc />
    public partial class AddIsOpenVenue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOpen",
                table: "Venues",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOpen",
                table: "Venues");
        }
    }
}
