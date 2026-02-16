using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramAds.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePaymentForInvoiceSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payments_InvoiceId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "payments");

            migrationBuilder.RenameColumn(
                name: "PaymentLink",
                table: "payments",
                newName: "PaymentUrl");

            migrationBuilder.RenameColumn(
                name: "OverpaymentInTon",
                table: "payments",
                newName: "ActualAmountInTon");

            migrationBuilder.AddColumn<string>(
                name: "InvoiceReference",
                table: "payments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InvoiceText",
                table: "payments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TransactionHash",
                table: "payments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TransactionTimestamp",
                table: "payments",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_InvoiceReference",
                table: "payments",
                column: "InvoiceReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_TransactionHash",
                table: "payments",
                column: "TransactionHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payments_InvoiceReference",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_TransactionHash",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "InvoiceReference",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "InvoiceText",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "TransactionHash",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "TransactionTimestamp",
                table: "payments");

            migrationBuilder.RenameColumn(
                name: "PaymentUrl",
                table: "payments",
                newName: "PaymentLink");

            migrationBuilder.RenameColumn(
                name: "ActualAmountInTon",
                table: "payments",
                newName: "OverpaymentInTon");

            migrationBuilder.AddColumn<string>(
                name: "InvoiceId",
                table: "payments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_payments_InvoiceId",
                table: "payments",
                column: "InvoiceId",
                unique: true);
        }
    }
}
