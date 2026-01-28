using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartMenuOptim.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddFilteredUniqueIndexToCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_Restaurant_UniqueName",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Restaurant_UniqueName",
                table: "Categories",
                columns: new[] { "RestaurantId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_Restaurant_UniqueName",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Restaurant_UniqueName",
                table: "Categories",
                columns: new[] { "RestaurantId", "Name" },
                unique: true);
        }
    }
}
