using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bn_Food_Ordering_Bot.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TelegramMessageId",
                table: "Orders",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TelegramMessageId",
                table: "Orders");
        }
    }
}
