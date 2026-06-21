using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    [DbContext(typeof(EduNestDbContext))]
    [Migration("20260621000000_AddTutorTranscriptDocument")]
    public partial class AddTutorTranscriptDocument : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "transcriptdocumentobjectkey",
                table: "tutors",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "transcriptdocumentobjectkey",
                table: "tutors");
        }
    }
}
