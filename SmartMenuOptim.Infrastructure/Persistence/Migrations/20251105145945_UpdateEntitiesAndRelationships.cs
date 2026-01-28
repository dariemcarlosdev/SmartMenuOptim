using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartMenuOptim.Shared.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntitiesAndRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StaffMembers_Restaurants_RestaurantId",
                table: "StaffMembers");

            migrationBuilder.DropIndex(
                name: "IX_StaffMembers_Email_Unique",
                table: "StaffMembers");

            migrationBuilder.DropIndex(
                name: "IX_StaffMembers_Restaurant_Role",
                table: "StaffMembers");

            migrationBuilder.DropIndex(
                name: "IX_StaffMembers_Username_Active",
                table: "StaffMembers");

            migrationBuilder.DropIndex(
                name: "IX_StaffMembers_Username_Unique",
                table: "StaffMembers");

            migrationBuilder.RenameIndex(
                name: "IX_StaffMembers_ApplicationUser",
                table: "StaffMembers",
                newName: "IX_StaffMembers_ApplicationUserId");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "BusinessRules",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Tables_Restaurant_TableNumber",
                table: "RestaurantTables",
                columns: new[] { "RestaurantId", "TableNumber" });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Area = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GrantedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AccessLevel = table.Column<int>(type: "integer", nullable: false),
                    RestaurantId = table.Column<int>(type: "integer", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Users_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuTypes_Restaurant_TimeRange",
                table: "MenuTypes",
                columns: new[] { "RestaurantId", "DefaultStartTime", "DefaultEndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_ApplicationUserId",
                table: "UserPermissions",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_Expiration_Active",
                table: "UserPermissions",
                columns: new[] { "ExpiresAt", "IsActive" },
                filter: "\"ExpiresAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_GrantedBy_Date",
                table: "UserPermissions",
                columns: new[] { "GrantedBy", "GrantedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_Restaurant_AccessLevel",
                table: "UserPermissions",
                columns: new[] { "RestaurantId", "AccessLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_Restaurant_User_Permission",
                table: "UserPermissions",
                columns: new[] { "RestaurantId", "ApplicationUserId", "Name" });

            migrationBuilder.AddForeignKey(
                name: "FK_StaffMembers_Restaurants_RestaurantId",
                table: "StaffMembers",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StaffMembers_Restaurants_RestaurantId",
                table: "StaffMembers");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Tables_Restaurant_TableNumber",
                table: "RestaurantTables");

            migrationBuilder.DropIndex(
                name: "IX_MenuTypes_Restaurant_TimeRange",
                table: "MenuTypes");

            migrationBuilder.RenameIndex(
                name: "IX_StaffMembers_ApplicationUserId",
                table: "StaffMembers",
                newName: "IX_StaffMembers_ApplicationUser");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "BusinessRules",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn);

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_Email_Unique",
                table: "StaffMembers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_Restaurant_Role",
                table: "StaffMembers",
                columns: new[] { "RestaurantId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_Username_Active",
                table: "StaffMembers",
                columns: new[] { "UserName", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_Username_Unique",
                table: "StaffMembers",
                column: "UserName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffMembers_Restaurants_RestaurantId",
                table: "StaffMembers",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
