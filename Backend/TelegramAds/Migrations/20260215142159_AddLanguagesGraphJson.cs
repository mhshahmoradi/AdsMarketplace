using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramAds.Migrations
{
    /// <inheritdoc />
    public partial class AddLanguagesGraphJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LanguagesGraphJson",
                table: "channel_stats",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LanguagesGraphJson",
                table: "channel_stats");
        }
    }
}
