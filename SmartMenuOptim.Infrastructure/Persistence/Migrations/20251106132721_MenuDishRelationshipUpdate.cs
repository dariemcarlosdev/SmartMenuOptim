using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartMenuOptim.Shared.Migrations
{
    /// <inheritdoc />
    public partial class MenuDishRelationshipUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuDishes_Dishes_DishId",
                table: "MenuDishes");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuDishes_Dishes_DishId1",
                table: "MenuDishes");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuDishes_Menus_MenuId",
                table: "MenuDishes");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuDishes_Menus_MenuId1",
                table: "MenuDishes");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuDishes_Restaurants_RestaurantId",
                table: "MenuDishes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MenuDishes",
                table: "MenuDishes");

            migrationBuilder.DropIndex(
                name: "IX_MenuDishes_DishId1",
                table: "MenuDishes");

            migrationBuilder.DropIndex(
                name: "IX_MenuDishes_MenuId1",
                table: "MenuDishes");

            migrationBuilder.DropColumn(
                name: "DishId1",
                table: "MenuDishes");

            migrationBuilder.DropColumn(
                name: "MenuId1",
                table: "MenuDishes");

            migrationBuilder.RenameTable(
                name: "MenuDishes",
                newName: "MenuDish");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "RestaurantTables",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Promotions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MenuDish",
                table: "MenuDish",
                columns: new[] { "MenuId", "DishId" });

            migrationBuilder.CreateTable(
                name: "DishMenu",
                columns: table => new
                {
                    DishesId = table.Column<int>(type: "integer", nullable: false),
                    MenusId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DishMenu", x => new { x.DishesId, x.MenusId });
                    table.ForeignKey(
                        name: "FK_DishMenu_Dishes_DishesId",
                        column: x => x.DishesId,
                        principalTable: "Dishes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DishMenu_Menus_MenusId",
                        column: x => x.MenusId,
                        principalTable: "Menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DishMenu_MenusId",
                table: "DishMenu",
                column: "MenusId");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuDish_Dishes_DishId",
                table: "MenuDish",
                column: "DishId",
                principalTable: "Dishes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuDish_Menus_MenuId",
                table: "MenuDish",
                column: "MenuId",
                principalTable: "Menus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuDish_Restaurants_RestaurantId",
                table: "MenuDish",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuDish_Dishes_DishId",
                table: "MenuDish");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuDish_Menus_MenuId",
                table: "MenuDish");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuDish_Restaurants_RestaurantId",
                table: "MenuDish");

            migrationBuilder.DropTable(
                name: "DishMenu");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MenuDish",
                table: "MenuDish");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "RestaurantTables");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Promotions");

            migrationBuilder.RenameTable(
                name: "MenuDish",
                newName: "MenuDishes");

            migrationBuilder.AddColumn<int>(
                name: "DishId1",
                table: "MenuDishes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MenuId1",
                table: "MenuDishes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MenuDishes",
                table: "MenuDishes",
                columns: new[] { "MenuId", "DishId" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuDishes_DishId1",
                table: "MenuDishes",
                column: "DishId1");

            migrationBuilder.CreateIndex(
                name: "IX_MenuDishes_MenuId1",
                table: "MenuDishes",
                column: "MenuId1");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuDishes_Dishes_DishId",
                table: "MenuDishes",
                column: "DishId",
                principalTable: "Dishes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuDishes_Dishes_DishId1",
                table: "MenuDishes",
                column: "DishId1",
                principalTable: "Dishes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuDishes_Menus_MenuId",
                table: "MenuDishes",
                column: "MenuId",
                principalTable: "Menus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuDishes_Menus_MenuId1",
                table: "MenuDishes",
                column: "MenuId1",
                principalTable: "Menus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuDishes_Restaurants_RestaurantId",
                table: "MenuDishes",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
