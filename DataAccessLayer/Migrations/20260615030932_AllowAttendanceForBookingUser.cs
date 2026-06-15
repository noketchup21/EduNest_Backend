using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AllowAttendanceForBookingUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "studentid",
                table: "attendances",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "userid",
                table: "attendances",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_attendances_lessonid_userid",
                table: "attendances",
                columns: new[] { "lessonid", "userid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_attendances_userid",
                table: "attendances",
                column: "userid");

            migrationBuilder.AddForeignKey(
                name: "fk_attendances_users_userid",
                table: "attendances",
                column: "userid",
                principalTable: "users",
                principalColumn: "userid",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_attendances_users_userid",
                table: "attendances");

            migrationBuilder.DropIndex(
                name: "ix_attendances_lessonid_userid",
                table: "attendances");

            migrationBuilder.DropIndex(
                name: "ix_attendances_userid",
                table: "attendances");

            migrationBuilder.DropColumn(
                name: "userid",
                table: "attendances");

            migrationBuilder.AlterColumn<int>(
                name: "studentid",
                table: "attendances",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
