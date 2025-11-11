using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduvisionMvc.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueEmailIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Simplify: disable FKs, repoint to canonical Ids (min per lowered email), delete duplicates, re-enable FKs.
            migrationBuilder.Sql(@"PRAGMA foreign_keys=OFF;
-- ==== STUDENTS DEDUP BY EMAIL ====
UPDATE AspNetUsers
SET StudentId = (
    SELECT MIN(s2.Id) FROM Students s2
    WHERE LOWER(s2.Email) = LOWER((SELECT s3.Email FROM Students s3 WHERE s3.Id = AspNetUsers.StudentId))
)
WHERE StudentId IS NOT NULL AND StudentId NOT IN (
    SELECT MIN(Id) FROM Students GROUP BY LOWER(Email)
);

UPDATE Enrollments
SET StudentId = (
    SELECT MIN(s2.Id) FROM Students s2
    WHERE LOWER(s2.Email) = LOWER((SELECT s3.Email FROM Students s3 WHERE s3.Id = Enrollments.StudentId))
)
WHERE StudentId NOT IN (
    SELECT MIN(Id) FROM Students GROUP BY LOWER(Email)
);

DELETE FROM Students
WHERE Id NOT IN (
    SELECT MIN(Id) FROM Students GROUP BY LOWER(Email)
);

-- ==== INSTRUCTORS DEDUP BY EMAIL ====
UPDATE AspNetUsers
SET InstructorId = (
    SELECT MIN(i2.Id) FROM Instructors i2
    WHERE LOWER(i2.Email) = LOWER((SELECT i3.Email FROM Instructors i3 WHERE i3.Id = AspNetUsers.InstructorId))
)
WHERE InstructorId IS NOT NULL AND InstructorId NOT IN (
    SELECT MIN(Id) FROM Instructors GROUP BY LOWER(Email)
);

UPDATE CourseInstructors
SET InstructorId = (
    SELECT MIN(i2.Id) FROM Instructors i2
    WHERE LOWER(i2.Email) = LOWER((SELECT i3.Email FROM Instructors i3 WHERE i3.Id = CourseInstructors.InstructorId))
)
WHERE InstructorId NOT IN (
    SELECT MIN(Id) FROM Instructors GROUP BY LOWER(Email)
);

DELETE FROM Instructors
WHERE Id NOT IN (
    SELECT MIN(Id) FROM Instructors GROUP BY LOWER(Email)
);
PRAGMA foreign_keys=ON;
");
            migrationBuilder.CreateIndex(
                name: "IX_Students_Email",
                table: "Students",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Instructors_Email",
                table: "Instructors",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_Email",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Instructors_Email",
                table: "Instructors");
        }
    }
}
