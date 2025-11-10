using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduvisionMvc.Migrations
{
    /// <inheritdoc />
    public partial class EnrichCoreTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcademicLevel",
                table: "Students",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AdvisorInstructorId",
                table: "Students",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Students",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "Instructors",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HireDate",
                table: "Instructors",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficeHours",
                table: "Instructors",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficeLocation",
                table: "Instructors",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Instructors",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Instructors",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttemptNumber",
                table: "Enrollments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsRepeatAttempt",
                table: "Enrollments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Enrollments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChairId",
                table: "Departments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Departments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficeLocation",
                table: "Departments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Departments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Departments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryMode",
                table: "Courses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Level",
                table: "Courses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Prerequisites",
                table: "Courses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_AdvisorInstructorId",
                table: "Students",
                column: "AdvisorInstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ChairId",
                table: "Departments",
                column: "ChairId");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Instructors_ChairId",
                table: "Departments",
                column: "ChairId",
                principalTable: "Instructors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Instructors_AdvisorInstructorId",
                table: "Students",
                column: "AdvisorInstructorId",
                principalTable: "Instructors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Instructors_ChairId",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Instructors_AdvisorInstructorId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_AdvisorInstructorId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Departments_ChairId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "AcademicLevel",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "AdvisorInstructorId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "Instructors");

            migrationBuilder.DropColumn(
                name: "HireDate",
                table: "Instructors");

            migrationBuilder.DropColumn(
                name: "OfficeHours",
                table: "Instructors");

            migrationBuilder.DropColumn(
                name: "OfficeLocation",
                table: "Instructors");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Instructors");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Instructors");

            migrationBuilder.DropColumn(
                name: "AttemptNumber",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "IsRepeatAttempt",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "ChairId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "OfficeLocation",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "DeliveryMode",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Prerequisites",
                table: "Courses");
        }
    }
}
