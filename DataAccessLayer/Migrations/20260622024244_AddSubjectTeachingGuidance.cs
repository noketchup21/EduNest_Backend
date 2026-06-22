using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectTeachingGuidance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "commondifficulties",
                table: "subjects",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "expectedresults",
                table: "subjects",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "learninggoals",
                table: "subjects",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "objective",
                table: "subjects",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "requiredtopics",
                table: "subjects",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "commondifficulties",
                table: "subjects");

            migrationBuilder.DropColumn(
                name: "expectedresults",
                table: "subjects");

            migrationBuilder.DropColumn(
                name: "learninggoals",
                table: "subjects");

            migrationBuilder.DropColumn(
                name: "objective",
                table: "subjects");

            migrationBuilder.DropColumn(
                name: "requiredtopics",
                table: "subjects");
        }
    }
}
