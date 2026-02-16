using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramAds.Migrations
{
    /// <inheritdoc />
    public partial class RefactorApplicationDealModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptanceStatus",
                table: "deals");

            migrationBuilder.AddColumn<Guid>(
                name: "CampaignApplicationId",
                table: "deals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ListingApplicationId",
                table: "deals",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "listing_applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProposedPriceInTon = table.Column<decimal>(type: "numeric(18,9)", precision: 18, scale: 9, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listing_applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_listing_applications_AspNetUsers_ApplicantUserId",
                        column: x => x.ApplicantUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_listing_applications_campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_listing_applications_listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_deals_CampaignApplicationId",
                table: "deals",
                column: "CampaignApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_deals_ListingApplicationId",
                table: "deals",
                column: "ListingApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_listing_applications_ApplicantUserId",
                table: "listing_applications",
                column: "ApplicantUserId");

            migrationBuilder.CreateIndex(
                name: "IX_listing_applications_CampaignId",
                table: "listing_applications",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_listing_applications_ListingId_CampaignId",
                table: "listing_applications",
                columns: new[] { "ListingId", "CampaignId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_deals_campaign_applications_CampaignApplicationId",
                table: "deals",
                column: "CampaignApplicationId",
                principalTable: "campaign_applications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_deals_listing_applications_ListingApplicationId",
                table: "deals",
                column: "ListingApplicationId",
                principalTable: "listing_applications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_deals_campaign_applications_CampaignApplicationId",
                table: "deals");

            migrationBuilder.DropForeignKey(
                name: "FK_deals_listing_applications_ListingApplicationId",
                table: "deals");

            migrationBuilder.DropTable(
                name: "listing_applications");

            migrationBuilder.DropIndex(
                name: "IX_deals_CampaignApplicationId",
                table: "deals");

            migrationBuilder.DropIndex(
                name: "IX_deals_ListingApplicationId",
                table: "deals");

            migrationBuilder.DropColumn(
                name: "CampaignApplicationId",
                table: "deals");

            migrationBuilder.DropColumn(
                name: "ListingApplicationId",
                table: "deals");

            migrationBuilder.AddColumn<string>(
                name: "AcceptanceStatus",
                table: "deals",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");
        }
    }
}
