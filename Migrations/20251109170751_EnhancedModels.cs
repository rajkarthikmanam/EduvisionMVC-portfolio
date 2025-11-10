using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduvisionMvc.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedDate",
                table: "Enrollments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAccessDate",
                table: "Enrollments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProgressPercentage",
                table: "Enrollments",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TotalHoursSpent",
                table: "Enrollments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "CourseMaterial",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "CourseMaterial",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "CourseMaterial",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "CourseMaterial",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "CourseMaterial",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Assignments",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                table: "Assignments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Assignments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxPoints",
                table: "Assignments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "LastAccessDate",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "ProgressPercentage",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "TotalHoursSpent",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "CourseMaterial");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "CourseMaterial");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "CourseMaterial");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "CourseMaterial");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "Instructions",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "MaxPoints",
                table: "Assignments");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "CourseMaterial",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
