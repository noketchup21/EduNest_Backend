using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseMaterialSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "fileurl",
                table: "materials",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "materials",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<string>(
                name: "contenttype",
                table: "materials",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "filename",
                table: "materials",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "filesize",
                table: "materials",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "materialsectionid",
                table: "materials",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "materialtype",
                table: "materials",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "updatedat",
                table: "materials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "materialsections",
                columns: table => new
                {
                    materialsectionid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    availabilityid = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    displayorder = table.Column<int>(type: "integer", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updatedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_materialsections", x => x.materialsectionid);
                    table.ForeignKey(
                        name: "fk_materialsections_availabilities_availabilityid",
                        column: x => x.availabilityid,
                        principalTable: "availabilities",
                        principalColumn: "availabilityid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_materials_materialsectionid",
                table: "materials",
                column: "materialsectionid");

            migrationBuilder.CreateIndex(
                name: "ix_materialsections_availabilityid",
                table: "materialsections",
                column: "availabilityid");

            migrationBuilder.AddForeignKey(
                name: "fk_materials_materialsections_materialsectionid",
                table: "materials",
                column: "materialsectionid",
                principalTable: "materialsections",
                principalColumn: "materialsectionid",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_materials_materialsections_materialsectionid",
                table: "materials");

            migrationBuilder.DropTable(
                name: "materialsections");

            migrationBuilder.DropIndex(
                name: "ix_materials_materialsectionid",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "contenttype",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "filename",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "filesize",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "materialsectionid",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "materialtype",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "updatedat",
                table: "materials");

            migrationBuilder.AlterColumn<string>(
                name: "fileurl",
                table: "materials",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "materials",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
