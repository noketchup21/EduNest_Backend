using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class FixAvailabilityTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
        ALTER TABLE availabilities
        ALTER COLUMN starttime TYPE time without time zone
        USING starttime::time;

        ALTER TABLE availabilities
        ALTER COLUMN endtime TYPE time without time zone
        USING endtime::time;
    """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
        ALTER TABLE availabilities
        ALTER COLUMN starttime TYPE interval
        USING (starttime - TIME '00:00:00');

        ALTER TABLE availabilities
        ALTER COLUMN endtime TYPE interval
        USING (endtime - TIME '00:00:00');
    """);
        }
    }
}
