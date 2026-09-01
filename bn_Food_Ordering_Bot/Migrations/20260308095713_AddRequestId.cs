using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bn_Food_Ordering_Bot.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestId",
                table: "Orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "Orders");
        }
    }
}
