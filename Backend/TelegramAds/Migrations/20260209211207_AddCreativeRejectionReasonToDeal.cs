using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramAds.Migrations
{
    /// <inheritdoc />
    public partial class AddCreativeRejectionReasonToDeal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreativeRejectionReason",
                table: "deals",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreativeRejectionReason",
                table: "deals");
        }
    }
}
