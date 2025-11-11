using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduvisionMvc.Migrations
{
    /// <inheritdoc />
    public partial class EnrichModelsWithNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseMaterial_AspNetUsers_UploadedById",
                table: "CourseMaterial");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseMaterial_Courses_CourseId",
                table: "CourseMaterial");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Instructors_AdvisorInstructorId",
                table: "Students");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CourseMaterial",
                table: "CourseMaterial");

            migrationBuilder.RenameTable(
                name: "CourseMaterial",
                newName: "CourseMaterials");

            migrationBuilder.RenameIndex(
                name: "IX_CourseMaterial_UploadedById",
                table: "CourseMaterials",
                newName: "IX_CourseMaterials_UploadedById");

            migrationBuilder.RenameIndex(
                name: "IX_CourseMaterial_CourseId",
                table: "CourseMaterials",
                newName: "IX_CourseMaterials_CourseId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CourseMaterials",
                table: "CourseMaterials",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseMaterials_AspNetUsers_UploadedById",
                table: "CourseMaterials",
                column: "UploadedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseMaterials_Courses_CourseId",
                table: "CourseMaterials",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Instructors_AdvisorInstructorId",
                table: "Students",
                column: "AdvisorInstructorId",
                principalTable: "Instructors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseMaterials_AspNetUsers_UploadedById",
                table: "CourseMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseMaterials_Courses_CourseId",
                table: "CourseMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Instructors_AdvisorInstructorId",
                table: "Students");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CourseMaterials",
                table: "CourseMaterials");

            migrationBuilder.RenameTable(
                name: "CourseMaterials",
                newName: "CourseMaterial");

            migrationBuilder.RenameIndex(
                name: "IX_CourseMaterials_UploadedById",
                table: "CourseMaterial",
                newName: "IX_CourseMaterial_UploadedById");

            migrationBuilder.RenameIndex(
                name: "IX_CourseMaterials_CourseId",
                table: "CourseMaterial",
                newName: "IX_CourseMaterial_CourseId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CourseMaterial",
                table: "CourseMaterial",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseMaterial_AspNetUsers_UploadedById",
                table: "CourseMaterial",
                column: "UploadedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseMaterial_Courses_CourseId",
                table: "CourseMaterial",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Instructors_AdvisorInstructorId",
                table: "Students",
                column: "AdvisorInstructorId",
                principalTable: "Instructors",
                principalColumn: "Id");
        }
    }
}
