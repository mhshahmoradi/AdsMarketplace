using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramAds.Migrations
{
    /// <inheritdoc />
    public partial class AddPremiumSubscriberCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PremiumSubscriberCount",
                table: "channel_stats",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PremiumSubscriberCount",
                table: "channel_stats");
        }
    }
}
