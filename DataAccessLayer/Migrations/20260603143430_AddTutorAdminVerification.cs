using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddTutorAdminVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cccdbackpublicid",
                table: "tutors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cccdfrontpublicid",
                table: "tutors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "certificatepublicid",
                table: "tutors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nationalidnumber",
                table: "tutors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "verificationrejectreason",
                table: "tutors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "verificationreviewedat",
                table: "tutors",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "verificationstatus",
                table: "tutors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "verificationsubmittedat",
                table: "tutors",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "bankname",
                table: "tutorbankaccounts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "accountholdername",
                table: "tutorbankaccounts",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "branchname",
                table: "tutorbankaccounts",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updatedat",
                table: "tutorbankaccounts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cccdbackpublicid",
                table: "tutors");

            migrationBuilder.DropColumn(
                name: "cccdfrontpublicid",
                table: "tutors");

            migrationBuilder.DropColumn(
                name: "certificatepublicid",
                table: "tutors");

            migrationBuilder.DropColumn(
                name: "nationalidnumber",
                table: "tutors");

            migrationBuilder.DropColumn(
                name: "verificationrejectreason",
                table: "tutors");

            migrationBuilder.DropColumn(
                name: "verificationreviewedat",
                table: "tutors");

            migrationBuilder.DropColumn(
                name: "verificationstatus",
                table: "tutors");

            migrationBuilder.DropColumn(
                name: "verificationsubmittedat",
                table: "tutors");

            migrationBuilder.DropColumn(
                name: "branchname",
                table: "tutorbankaccounts");

            migrationBuilder.DropColumn(
                name: "updatedat",
                table: "tutorbankaccounts");

            migrationBuilder.AlterColumn<string>(
                name: "bankname",
                table: "tutorbankaccounts",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "accountholdername",
                table: "tutorbankaccounts",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);
        }
    }
}
