using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminDashboardMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "appmetrics",
                columns: table => new
                {
                    appmetricid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    deviceid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    appversion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_appmetrics", x => x.appmetricid);
                });

            migrationBuilder.CreateIndex(
                name: "ix_appmetrics_type_deviceid",
                table: "appmetrics",
                columns: new[] { "type", "deviceid" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appmetrics");
        }
    }
}
