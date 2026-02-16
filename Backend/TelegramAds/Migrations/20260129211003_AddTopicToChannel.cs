using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramAds.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicToChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "channels",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Topic",
                table: "channels");
        }
    }
}
