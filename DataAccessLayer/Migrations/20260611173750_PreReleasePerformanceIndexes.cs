using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class PreReleasePerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_materialsections_availabilityid",
                table: "materialsections");

            migrationBuilder.DropIndex(
                name: "ix_materials_availabilityid",
                table: "materials");

            migrationBuilder.DropIndex(
                name: "ix_lessons_bookingid",
                table: "lessons");

            migrationBuilder.DropIndex(
                name: "ix_homeworks_bookingid",
                table: "homeworks");

            migrationBuilder.DropIndex(
                name: "ix_homeworks_lessonid",
                table: "homeworks");

            migrationBuilder.CreateIndex(
                name: "ix_submissions_homeworkid_submittedat",
                table: "submissions",
                columns: new[] { "homeworkid", "submittedat" });

            migrationBuilder.CreateIndex(
                name: "ix_materialsections_availabilityid_displayorder",
                table: "materialsections",
                columns: new[] { "availabilityid", "displayorder" });

            migrationBuilder.CreateIndex(
                name: "ix_materials_availabilityid_materialsectionid_createdat",
                table: "materials",
                columns: new[] { "availabilityid", "materialsectionid", "createdat" });

            migrationBuilder.CreateIndex(
                name: "ix_lessons_bookingid_scheduletime",
                table: "lessons",
                columns: new[] { "bookingid", "scheduletime" });

            migrationBuilder.CreateIndex(
                name: "ix_homeworks_bookingid_uploadedat",
                table: "homeworks",
                columns: new[] { "bookingid", "uploadedat" });

            migrationBuilder.CreateIndex(
                name: "ix_homeworks_duedate",
                table: "homeworks",
                column: "duedate");

            migrationBuilder.CreateIndex(
                name: "ix_homeworks_lessonid_uploadedat",
                table: "homeworks",
                columns: new[] { "lessonid", "uploadedat" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_submissions_homeworkid_submittedat",
                table: "submissions");

            migrationBuilder.DropIndex(
                name: "ix_materialsections_availabilityid_displayorder",
                table: "materialsections");

            migrationBuilder.DropIndex(
                name: "ix_materials_availabilityid_materialsectionid_createdat",
                table: "materials");

            migrationBuilder.DropIndex(
                name: "ix_lessons_bookingid_scheduletime",
                table: "lessons");

            migrationBuilder.DropIndex(
                name: "ix_homeworks_bookingid_uploadedat",
                table: "homeworks");

            migrationBuilder.DropIndex(
                name: "ix_homeworks_duedate",
                table: "homeworks");

            migrationBuilder.DropIndex(
                name: "ix_homeworks_lessonid_uploadedat",
                table: "homeworks");

            migrationBuilder.CreateIndex(
                name: "ix_materialsections_availabilityid",
                table: "materialsections",
                column: "availabilityid");

            migrationBuilder.CreateIndex(
                name: "ix_materials_availabilityid",
                table: "materials",
                column: "availabilityid");

            migrationBuilder.CreateIndex(
                name: "ix_lessons_bookingid",
                table: "lessons",
                column: "bookingid");

            migrationBuilder.CreateIndex(
                name: "ix_homeworks_bookingid",
                table: "homeworks",
                column: "bookingid");

            migrationBuilder.CreateIndex(
                name: "ix_homeworks_lessonid",
                table: "homeworks",
                column: "lessonid");
        }
    }
}
