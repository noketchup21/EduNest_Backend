using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddTutorReportSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tutorreports",
                columns: table => new
                {
                    tutorreportid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reporteruserid = table.Column<int>(type: "integer", nullable: false),
                    tutorid = table.Column<int>(type: "integer", nullable: false),
                    bookingid = table.Column<int>(type: "integer", nullable: false),
                    availabilityid = table.Column<int>(type: "integer", nullable: false),
                    lessonid = table.Column<int>(type: "integer", nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    adminnote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reviewedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tutorreports", x => x.tutorreportid);
                    table.ForeignKey(
                        name: "fk_tutorreports_availabilities_availabilityid",
                        column: x => x.availabilityid,
                        principalTable: "availabilities",
                        principalColumn: "availabilityid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tutorreports_bookings_bookingid",
                        column: x => x.bookingid,
                        principalTable: "bookings",
                        principalColumn: "bookingid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tutorreports_lessons_lessonid",
                        column: x => x.lessonid,
                        principalTable: "lessons",
                        principalColumn: "lessonid",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tutorreports_tutors_tutorid",
                        column: x => x.tutorid,
                        principalTable: "tutors",
                        principalColumn: "tutorid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tutorreports_users_reporteruserid",
                        column: x => x.reporteruserid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tutorreportproofimages",
                columns: table => new
                {
                    tutorreportproofimageid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tutorreportid = table.Column<int>(type: "integer", nullable: false),
                    publicid = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tutorreportproofimages", x => x.tutorreportproofimageid);
                    table.ForeignKey(
                        name: "fk_tutorreportproofimages_tutorreports_tutorreportid",
                        column: x => x.tutorreportid,
                        principalTable: "tutorreports",
                        principalColumn: "tutorreportid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tutorreportproofimages_tutorreportid",
                table: "tutorreportproofimages",
                column: "tutorreportid");

            migrationBuilder.CreateIndex(
                name: "ix_tutorreports_availabilityid",
                table: "tutorreports",
                column: "availabilityid");

            migrationBuilder.CreateIndex(
                name: "ix_tutorreports_bookingid",
                table: "tutorreports",
                column: "bookingid");

            migrationBuilder.CreateIndex(
                name: "ix_tutorreports_createdat",
                table: "tutorreports",
                column: "createdat");

            migrationBuilder.CreateIndex(
                name: "ix_tutorreports_lessonid",
                table: "tutorreports",
                column: "lessonid");

            migrationBuilder.CreateIndex(
                name: "ix_tutorreports_reporteruserid_bookingid",
                table: "tutorreports",
                columns: new[] { "reporteruserid", "bookingid" });

            migrationBuilder.CreateIndex(
                name: "ix_tutorreports_tutorid_status",
                table: "tutorreports",
                columns: new[] { "tutorid", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tutorreportproofimages");

            migrationBuilder.DropTable(
                name: "tutorreports");
        }
    }
}
