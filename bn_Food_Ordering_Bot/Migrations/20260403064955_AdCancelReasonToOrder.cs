using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bn_Food_Ordering_Bot.Migrations
{
    /// <inheritdoc />
    public partial class AdCancelReasonToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "Orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "Orders");
        }
    }
}
