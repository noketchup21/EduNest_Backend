using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDbMVP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "createdat_decimal",
                table: "payouts");

            migrationBuilder.Sql("""
    ALTER TABLE payouts
    ALTER COLUMN amount TYPE numeric(18,2)
    USING amount::numeric(18,2);
""");

            migrationBuilder.AddColumn<DateTime>(
                name: "requestedat",
                table: "payouts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "checkouturl",
                table: "payments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "paidat",
                table: "payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider",
                table: "payments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "providerordercode",
                table: "payments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "qrcode",
                table: "payments",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "studentid",
                table: "bookings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "parentid",
                table: "bookings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "userid",
                table: "bookings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_bookings_userid",
                table: "bookings",
                column: "userid");

            migrationBuilder.AddForeignKey(
                name: "fk_bookings_users_userid",
                table: "bookings",
                column: "userid",
                principalTable: "users",
                principalColumn: "userid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_bookings_users_userid",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "ix_bookings_userid",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "requestedat",
                table: "payouts");

            migrationBuilder.DropColumn(
                name: "checkouturl",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "description",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "paidat",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "provider",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "providerordercode",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "qrcode",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "userid",
                table: "bookings");

            migrationBuilder.AlterColumn<string>(
                name: "amount",
                table: "payouts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AddColumn<decimal>(
                name: "createdat_decimal",
                table: "payouts",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "studentid",
                table: "bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "parentid",
                table: "bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
