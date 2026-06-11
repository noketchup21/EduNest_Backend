using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddMobileTutorEngagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "userid",
                table: "reviews",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "parentid",
                table: "favoritetutors",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "userid",
                table: "favoritetutors",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_reviews_userid_bookingid",
                table: "reviews",
                columns: new[] { "userid", "bookingid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_favoritetutors_userid_tutorid",
                table: "favoritetutors",
                columns: new[] { "userid", "tutorid" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_favoritetutors_users_userid",
                table: "favoritetutors",
                column: "userid",
                principalTable: "users",
                principalColumn: "userid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_reviews_users_userid",
                table: "reviews",
                column: "userid",
                principalTable: "users",
                principalColumn: "userid",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_favoritetutors_users_userid",
                table: "favoritetutors");

            migrationBuilder.DropForeignKey(
                name: "fk_reviews_users_userid",
                table: "reviews");

            migrationBuilder.DropIndex(
                name: "ix_reviews_userid_bookingid",
                table: "reviews");

            migrationBuilder.DropIndex(
                name: "ix_favoritetutors_userid_tutorid",
                table: "favoritetutors");

            migrationBuilder.DropColumn(
                name: "userid",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "userid",
                table: "favoritetutors");

            migrationBuilder.AlterColumn<int>(
                name: "parentid",
                table: "favoritetutors",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
