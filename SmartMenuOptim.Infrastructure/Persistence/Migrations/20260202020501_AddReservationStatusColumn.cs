using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartMenuOptim.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationStatusColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SaleAmount",
                table: "SaleRecords",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Reservations",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "0=Pending, 1=Confirmed, 2=Seated, 3=Completed, 4=Cancelled, 5=NoShow");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Restaurant_Status_Time",
                table: "Reservations",
                columns: new[] { "RestaurantId", "Status", "ReservationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Table_Status_Time",
                table: "Reservations",
                columns: new[] { "TableId", "Status", "ReservationTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_Restaurant_Status_Time",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_Table_Status_Time",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Reservations");

            migrationBuilder.AlterColumn<string>(
                name: "SaleAmount",
                table: "SaleRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
        }
    }
}
