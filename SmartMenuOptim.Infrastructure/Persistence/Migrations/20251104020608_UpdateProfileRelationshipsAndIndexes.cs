using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartMenuOptim.Shared.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProfileRelationshipsAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Restaurants_RestaurantTenantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUsers_Email_Unique",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUsers_Tenant_ProfileType",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUsers_Username_Unique",
                table: "Users");

            migrationBuilder.RenameIndex(
                name: "IX_StaffMembers_ApplicationUserId",
                table: "StaffMembers",
                newName: "IX_StaffMembers_ApplicationUser");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_ApplicationUserId",
                table: "Customers",
                newName: "IX_Customers_ApplicationUser");

            migrationBuilder.RenameIndex(
                name: "IX_AdminUsers_ApplicationUserId",
                table: "AdminUsers",
                newName: "IX_AdminUsers_ApplicationUser");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_Profile",
                table: "Users",
                columns: new[] { "ProfileType", "ProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_Profile_Tenant",
                table: "Users",
                columns: new[] { "ProfileType", "RestaurantTenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_RestaurantTenantId",
                table: "Users",
                column: "RestaurantTenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Restaurants_RestaurantTenantId",
                table: "Users",
                column: "RestaurantTenantId",
                principalTable: "Restaurants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Restaurants_RestaurantTenantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUsers_Profile",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUsers_Profile_Tenant",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_RestaurantTenantId",
                table: "Users");

            migrationBuilder.RenameIndex(
                name: "IX_StaffMembers_ApplicationUser",
                table: "StaffMembers",
                newName: "IX_StaffMembers_ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_ApplicationUser",
                table: "Customers",
                newName: "IX_Customers_ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_AdminUsers_ApplicationUser",
                table: "AdminUsers",
                newName: "IX_AdminUsers_ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_Email_Unique",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_Tenant_ProfileType",
                table: "Users",
                columns: new[] { "RestaurantTenantId", "ProfileType" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_Username_Unique",
                table: "Users",
                column: "UserName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Restaurants_RestaurantTenantId",
                table: "Users",
                column: "RestaurantTenantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
