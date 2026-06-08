using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportReportSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supportreports",
                columns: table => new
                {
                    supportreportid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    payoutid = table.Column<int>(type: "integer", nullable: true),
                    bookingid = table.Column<int>(type: "integer", nullable: true),
                    lessonid = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    adminnote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reviewedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supportreports", x => x.supportreportid);
                    table.ForeignKey(
                        name: "fk_supportreports_users_userid",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supportreportproofimages",
                columns: table => new
                {
                    supportreportproofimageid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    supportreportid = table.Column<int>(type: "integer", nullable: false),
                    publicid = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supportreportproofimages", x => x.supportreportproofimageid);
                    table.ForeignKey(
                        name: "fk_supportreportproofimages_supportreports_supportreportid",
                        column: x => x.supportreportid,
                        principalTable: "supportreports",
                        principalColumn: "supportreportid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_supportreportproofimages_supportreportid",
                table: "supportreportproofimages",
                column: "supportreportid");

            migrationBuilder.CreateIndex(
                name: "ix_supportreports_createdat",
                table: "supportreports",
                column: "createdat");

            migrationBuilder.CreateIndex(
                name: "ix_supportreports_role_status",
                table: "supportreports",
                columns: new[] { "role", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_supportreports_userid_status",
                table: "supportreports",
                columns: new[] { "userid", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supportreportproofimages");

            migrationBuilder.DropTable(
                name: "supportreports");
        }
    }
}
