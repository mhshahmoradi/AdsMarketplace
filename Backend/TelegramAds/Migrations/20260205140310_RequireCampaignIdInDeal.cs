using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramAds.Migrations
{
    /// <inheritdoc />
    public partial class RequireCampaignIdInDeal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_deals_campaign_applications_CampaignApplicationId",
                table: "deals");

            migrationBuilder.DropIndex(
                name: "IX_deals_CampaignApplicationId",
                table: "deals");

            migrationBuilder.DropColumn(
                name: "CampaignApplicationId",
                table: "deals");

            migrationBuilder.AddColumn<Guid>(
                name: "CampaignId",
                table: "deals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "ToStatus",
                table: "deal_events",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "FromStatus",
                table: "deal_events",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "deal_events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_deals_CampaignId",
                table: "deals",
                column: "CampaignId");

            migrationBuilder.AddForeignKey(
                name: "FK_deals_campaigns_CampaignId",
                table: "deals",
                column: "CampaignId",
                principalTable: "campaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_deals_campaigns_CampaignId",
                table: "deals");

            migrationBuilder.DropIndex(
                name: "IX_deals_CampaignId",
                table: "deals");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "deals");

            migrationBuilder.AddColumn<Guid>(
                name: "CampaignApplicationId",
                table: "deals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ToStatus",
                table: "deal_events",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FromStatus",
                table: "deal_events",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "deal_events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.CreateIndex(
                name: "IX_deals_CampaignApplicationId",
                table: "deals",
                column: "CampaignApplicationId");

            migrationBuilder.AddForeignKey(
                name: "FK_deals_campaign_applications_CampaignApplicationId",
                table: "deals",
                column: "CampaignApplicationId",
                principalTable: "campaign_applications",
                principalColumn: "Id");
        }
    }
}
