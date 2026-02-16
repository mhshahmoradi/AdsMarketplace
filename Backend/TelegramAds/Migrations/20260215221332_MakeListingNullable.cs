using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramAds.Migrations
{
    /// <inheritdoc />
    public partial class MakeListingNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_deals_channels_ChannelId",
                table: "deals");

            migrationBuilder.DropForeignKey(
                name: "FK_deals_listings_ListingId",
                table: "deals");

            migrationBuilder.AlterColumn<Guid>(
                name: "ListingId",
                table: "deals",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ChannelId",
                table: "deals",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "CampaignId",
                table: "deals",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_deals_channels_ChannelId",
                table: "deals",
                column: "ChannelId",
                principalTable: "channels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_deals_listings_ListingId",
                table: "deals",
                column: "ListingId",
                principalTable: "listings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_deals_channels_ChannelId",
                table: "deals");

            migrationBuilder.DropForeignKey(
                name: "FK_deals_listings_ListingId",
                table: "deals");

            migrationBuilder.AlterColumn<Guid>(
                name: "ListingId",
                table: "deals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ChannelId",
                table: "deals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CampaignId",
                table: "deals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_deals_channels_ChannelId",
                table: "deals",
                column: "ChannelId",
                principalTable: "channels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_deals_listings_ListingId",
                table: "deals",
                column: "ListingId",
                principalTable: "listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
