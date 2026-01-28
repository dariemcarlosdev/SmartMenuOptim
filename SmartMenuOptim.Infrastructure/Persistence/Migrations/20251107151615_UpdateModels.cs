using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartMenuOptim.Shared.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropPrimaryKey(
                name: "PK_MenuDish",
                table: "MenuDish");

            migrationBuilder.RenameTable(
                name: "MenuDish",
                newName: "MenuDishes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MenuDishes",
                table: "MenuDishes",
                columns: new[] { "MenuId", "DishId" });

            migrationBuilder.AddForeignKey(
                name: "FK_MenuDishes_Dishes_DishId",
                table: "MenuDishes",
                column: "DishId",
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
                name: "FK_MenuDishes_Restaurants_RestaurantId",
                table: "MenuDishes",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuDishes_Dishes_DishId",
                table: "MenuDishes");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuDishes_Menus_MenuId",
                table: "MenuDishes");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuDishes_Restaurants_RestaurantId",
                table: "MenuDishes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MenuDishes",
                table: "MenuDishes");

            migrationBuilder.RenameTable(
                name: "MenuDishes",
                newName: "MenuDish");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MenuDish",
                table: "MenuDish",
                columns: new[] { "MenuId", "DishId" });

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
    }
}
