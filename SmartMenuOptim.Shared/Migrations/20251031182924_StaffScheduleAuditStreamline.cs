using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartMenuOptim.Shared.Migrations
{
    /// <inheritdoc />
    public partial class StaffScheduleAuditStreamline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StaffSchedules_StaffMembers_CreatedByStaffId",
                table: "StaffSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffSchedules_StaffMembers_LastModifiedById",
                table: "StaffSchedules");

            migrationBuilder.DropIndex(
                name: "IX_StaffSchedules_CreatedByStaffId",
                table: "StaffSchedules");

            migrationBuilder.DropIndex(
                name: "IX_StaffSchedules_LastModifiedById",
                table: "StaffSchedules");

            migrationBuilder.DropColumn(
                name: "CreatedByStaffId",
                table: "StaffSchedules");

            migrationBuilder.DropColumn(
                name: "LastModifiedById",
                table: "StaffSchedules");

            migrationBuilder.DropColumn(
                name: "LastModifiedByStaffId",
                table: "StaffSchedules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByStaffId",
                table: "StaffSchedules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastModifiedById",
                table: "StaffSchedules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastModifiedByStaffId",
                table: "StaffSchedules",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffSchedules_CreatedByStaffId",
                table: "StaffSchedules",
                column: "CreatedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffSchedules_LastModifiedById",
                table: "StaffSchedules",
                column: "LastModifiedById");

            migrationBuilder.AddForeignKey(
                name: "FK_StaffSchedules_StaffMembers_CreatedByStaffId",
                table: "StaffSchedules",
                column: "CreatedByStaffId",
                principalTable: "StaffMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffSchedules_StaffMembers_LastModifiedById",
                table: "StaffSchedules",
                column: "LastModifiedById",
                principalTable: "StaffMembers",
                principalColumn: "Id");
        }
    }
}
