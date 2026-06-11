using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeworkSessionManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "studentid",
                table: "submissions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "feedback",
                table: "submissions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "gradedat",
                table: "submissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "userid",
                table: "submissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "lessonid",
                table: "homeworks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "homeworks",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "MultipleChoice");

            migrationBuilder.CreateIndex(
                name: "ix_submissions_homeworkid_userid",
                table: "submissions",
                columns: new[] { "homeworkid", "userid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_submissions_userid",
                table: "submissions",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "ix_homeworks_lessonid",
                table: "homeworks",
                column: "lessonid");

            migrationBuilder.AddForeignKey(
                name: "fk_homeworks_lessons_lessonid",
                table: "homeworks",
                column: "lessonid",
                principalTable: "lessons",
                principalColumn: "lessonid",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_submissions_users_userid",
                table: "submissions",
                column: "userid",
                principalTable: "users",
                principalColumn: "userid",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_homeworks_lessons_lessonid",
                table: "homeworks");

            migrationBuilder.DropForeignKey(
                name: "fk_submissions_users_userid",
                table: "submissions");

            migrationBuilder.DropIndex(
                name: "ix_submissions_homeworkid_userid",
                table: "submissions");

            migrationBuilder.DropIndex(
                name: "ix_submissions_userid",
                table: "submissions");

            migrationBuilder.DropIndex(
                name: "ix_homeworks_lessonid",
                table: "homeworks");

            migrationBuilder.DropColumn(
                name: "feedback",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "gradedat",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "userid",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "lessonid",
                table: "homeworks");

            migrationBuilder.DropColumn(
                name: "type",
                table: "homeworks");

            migrationBuilder.AlterColumn<int>(
                name: "studentid",
                table: "submissions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
