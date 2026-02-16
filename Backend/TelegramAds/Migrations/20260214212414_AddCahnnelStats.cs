using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramAds.Migrations
{
    /// <inheritdoc />
    public partial class AddCahnnelStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "telegram_sessions");

            migrationBuilder.DropColumn(
                name: "LanguageDistributionJson",
                table: "channel_stats");

            migrationBuilder.AddColumn<double>(
                name: "AvgReactionsPerPost",
                table: "channel_stats",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "AvgReactionsPerStory",
                table: "channel_stats",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "AvgSharesPerPost",
                table: "channel_stats",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "AvgSharesPerStory",
                table: "channel_stats",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "AvgViewsPerStory",
                table: "channel_stats",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EnabledNotificationsPercent",
                table: "channel_stats",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "FollowersCount",
                table: "channel_stats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FollowersPrevCount",
                table: "channel_stats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StatsEndDate",
                table: "channel_stats",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StatsStartDate",
                table: "channel_stats",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvgReactionsPerPost",
                table: "channel_stats");

            migrationBuilder.DropColumn(
                name: "AvgReactionsPerStory",
                table: "channel_stats");

            migrationBuilder.DropColumn(
                name: "AvgSharesPerPost",
                table: "channel_stats");

            migrationBuilder.DropColumn(
                name: "AvgSharesPerStory",
                table: "channel_stats");

            migrationBuilder.DropColumn(
                name: "AvgViewsPerStory",
                table: "channel_stats");

            migrationBuilder.DropColumn(
                name: "EnabledNotificationsPercent",
                table: "channel_stats");

            migrationBuilder.DropColumn(
                name: "FollowersCount",
                table: "channel_stats");

            migrationBuilder.DropColumn(
                name: "FollowersPrevCount",
                table: "channel_stats");

            migrationBuilder.DropColumn(
                name: "StatsEndDate",
                table: "channel_stats");

            migrationBuilder.DropColumn(
                name: "StatsStartDate",
                table: "channel_stats");

            migrationBuilder.AddColumn<string>(
                name: "LanguageDistributionJson",
                table: "channel_stats",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "telegram_sessions",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    data = table.Column<byte[]>(type: "bytea", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telegram_sessions", x => x.name);
                });
        }
    }
}
