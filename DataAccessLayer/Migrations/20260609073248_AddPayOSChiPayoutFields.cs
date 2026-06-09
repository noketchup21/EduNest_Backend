using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddPayOSChiPayoutFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "approvedat",
                table: "payouts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payoschiapprovalstate",
                table: "payouts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payoschibatchid",
                table: "payouts",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payoschifailurereason",
                table: "payouts",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payoschipayoutitemid",
                table: "payouts",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payoschireferenceid",
                table: "payouts",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payoschitransactionstate",
                table: "payouts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payoutmethod",
                table: "payouts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "approvedat",
                table: "payouts");

            migrationBuilder.DropColumn(
                name: "payoschiapprovalstate",
                table: "payouts");

            migrationBuilder.DropColumn(
                name: "payoschibatchid",
                table: "payouts");

            migrationBuilder.DropColumn(
                name: "payoschifailurereason",
                table: "payouts");

            migrationBuilder.DropColumn(
                name: "payoschipayoutitemid",
                table: "payouts");

            migrationBuilder.DropColumn(
                name: "payoschireferenceid",
                table: "payouts");

            migrationBuilder.DropColumn(
                name: "payoschitransactionstate",
                table: "payouts");

            migrationBuilder.DropColumn(
                name: "payoutmethod",
                table: "payouts");
        }
    }
}
