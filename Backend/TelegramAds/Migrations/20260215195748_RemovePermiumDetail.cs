using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramAds.Migrations
{
    /// <inheritdoc />
    public partial class RemovePermiumDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PremiumSubscriberShare",
                table: "channel_stats");

            migrationBuilder.AlterColumn<double>(
                name: "PremiumSubscriberCount",
                table: "channel_stats",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "PremiumSubscriberCount",
                table: "channel_stats",
                type: "integer",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AddColumn<double>(
                name: "PremiumSubscriberShare",
                table: "channel_stats",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
