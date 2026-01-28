using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartMenuOptim.Shared.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCustomerIdFromLoyaltyTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LoyaltyTransactions_Customers_CustomerId",
                table: "LoyaltyTransactions");

            migrationBuilder.DropIndex(
                name: "IX_LoyaltyTransactions_CustomerId",
                table: "LoyaltyTransactions");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "LoyaltyTransactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "LoyaltyTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_CustomerId",
                table: "LoyaltyTransactions",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_LoyaltyTransactions_Customers_CustomerId",
                table: "LoyaltyTransactions",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
