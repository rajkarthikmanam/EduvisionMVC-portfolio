# 📚 EduVision MVC - Entity Relationship Diagram (ERD)

## Core Entities & Relationships

### **ApplicationUser** (extends IdentityUser)
Identity framework user entity with custom properties.

**Properties:**
- `Id` (string, PK) - Identity GUID
- `UserName`, `Email`, `PasswordHash` (from IdentityUser)
- `FirstName`, `LastName` - User display name
- `Avatar` - Profile picture URL
- `CreatedAt`, `LastLoginDate` - Audit timestamps
- `InstructorId?` (int, FK to Instructor) - Nullable, links if user is instructor
- `StudentId?` (int, FK to Student) - Nullable, links if user is student
- `IsActive`, `DateJoined`

**Relationships:**
- 1:1 optional → Instructor (via `InstructorId`)
- 1:1 optional → Student (via `StudentId`)
- 1:N → Notifications
- 1:N → UploadedMaterials
- 1:N → EnrollmentChanges (audit trail)
- 1:N → CourseAnnouncements
- 1:N → DiscussionPosts

---

### **Department**
Academic department managing courses and instructors.

**Properties:**
- `Id` (int, PK)
- `Name` - Department name (e.g., "Computer Science")
- `Code` - Department abbreviation (e.g., "CS")

**Relationships:**
- 1:N → Courses
- 1:N → Instructors

**Constraints:**
- Unique: (Code)

---

### **Instructor**
Faculty member linked to ApplicationUser.

**Properties:**
- `Id` (int, PK)
- `FirstName`, `LastName`, `Email`
- `DepartmentId` (FK to Department)
- `UserId` (FK to ApplicationUser)

**Relationships:**
- N:1 → Department
- 1:1 → ApplicationUser (via `UserId`)
- M:N → Courses (via CourseInstructor)

---

### **Student**
Student entity linked to ApplicationUser.

**Properties:**
- `Id` (int, PK)
- `Name`, `Email`, `Major`
- `Gpa` (decimal, precision 3,2)
- `TotalCredits`, `Age`
- `EnrollmentDate`
- `UserId` (FK to ApplicationUser)

**Relationships:**
- 1:1 → ApplicationUser (via `UserId`)
- 1:N → Enrollments
- 1:N → AssignmentSubmissions

---

### **Course**
Academic course offering.

**Properties:**
- `Id` (int, PK)
- `Code` - Course code (e.g., "CS101")
- `Title`, `Description`
- `Credits`, `Capacity`, `Schedule`, `Location`
- `RequiresApproval` (bool)
- `StartDate`, `EndDate`
- `DepartmentId` (FK to Department)

**Relationships:**
- N:1 → Department
- M:N → Instructors (via CourseInstructor)
- 1:N → Enrollments
- 1:N → CourseMaterials
- 1:N → Assignments
- 1:N → CourseAnnouncements
- 1:N → Discussions

**Constraints:**
- Unique: (Code, DepartmentId) - course code unique within department

---

### **CourseInstructor** (Join Table)
Many-to-many relationship between Courses and Instructors.

**Properties:**
- `CourseId` (FK to Course, composite PK)
- `InstructorId` (FK to Instructor, composite PK)

**Constraints:**
- Composite Primary Key: (CourseId, InstructorId)

---

### **Enrollment**
Student enrollment in a course with progress tracking.

**Properties:**
- `Id` (int, PK)
- `StudentId` (FK to Student)
- `CourseId` (FK to Course)
- `Term` - Enrollment term (e.g., "Fall 2025")
- `Status` (enum: Pending, Approved, Dropped, Completed)
- `Numeric_Grade` (decimal, nullable)
- `LetterGrade` (string, nullable)
- **Progress fields:**
  - `ProgressPercentage` (decimal, 0-100)
  - `LastAccessDate` (DateTime?, nullable)
  - `TotalHoursSpent` (int, default 0)
- **Timestamps:**
  - `EnrolledDate`, `ApprovedDate`, `DroppedDate`, `CompletedDate`

**Relationships:**
- N:1 → Student
- N:1 → Course
- 1:N → EnrollmentHistory

**Constraints:**
- Unique: (StudentId, CourseId, Term) - one enrollment per student per course per term

---

### **EnrollmentHistory**
Audit trail for enrollment changes.

**Properties:**
- `Id` (int, PK)
- `EnrollmentId` (FK to Enrollment)
- `Action` - Description of change (e.g., "Enrollment Approved")
- `Timestamp`
- `UpdatedById` (FK to ApplicationUser)
- `OldGrade`, `NewGrade` (decimal?, nullable)

**Relationships:**
- N:1 → Enrollment
- N:1 → ApplicationUser (UpdatedBy)

**Delete Behavior:**
- OnDelete: Restrict (UpdatedBy)

---

### **CourseMaterial**
Learning resources uploaded for courses.

**Properties:**
- `Id` (int, PK)
- `Title`, `Description`
- `Url` (string?, nullable) - External link
- `FilePath` (string?, nullable) - Server path if uploaded
- `Type` (enum: Document, Video, Link, Presentation, Code, Other)
- `FileSize` (long, bytes)
- `IsPublished` (bool) - Draft vs. published state
- `UploadedDate`
- `CourseId` (FK to Course)
- `UploadedById` (FK to ApplicationUser)

**Relationships:**
- N:1 → Course
- N:1 → ApplicationUser (UploadedBy)

**Delete Behavior:**
- OnDelete: Restrict (UploadedBy)

---

### **Assignment**
Course assignment with submission tracking.

**Properties:**
- `Id` (int, PK)
- `CourseId` (FK to Course)
- `Title`, `Description`, `Instructions`
- `CreatedDate`, `DueDate` (DateTime?)
- `MaxPoints` (int)
- `IsPublished` (bool)

**Relationships:**
- N:1 → Course
- 1:N → AssignmentSubmissions

---

### **AssignmentSubmission**
Student submission for an assignment.

**Properties:**
- `Id` (int, PK)
- `AssignmentId` (FK to Assignment)
- `StudentId` (FK to Student)
- `SubmittedAt`
- `Grade` (decimal?, nullable)
- `Content` - Submission text/notes

**Relationships:**
- N:1 → Assignment
- N:1 → Student

---

### **CourseAnnouncement**
Instructor announcements for courses.

**Properties:**
- `Id` (int, PK)
- `CourseId` (FK to Course)
- `Title`, `Content`
- `CreatedAt`
- `AuthorId` (FK to ApplicationUser)

**Relationships:**
- N:1 → Course
- N:1 → ApplicationUser (Author)

---

### **Discussion**
Course discussion topic/thread.

**Properties:**
- `Id` (int, PK)
- `Title`
- `CourseId` (FK to Course)

**Relationships:**
- N:1 → Course
- 1:N → DiscussionPosts

---

### **DiscussionPost**
Individual post in a discussion thread.

**Properties:**
- `Id` (int, PK)
- `Content`
- `PostedAt`
- `DiscussionId` (FK to Discussion)
- `AuthorId` (FK to ApplicationUser)

**Relationships:**
- N:1 → Discussion
- N:1 → ApplicationUser (Author)

---

### **Notification**
User notifications.

**Properties:**
- `Id` (int, PK)
- `Message`
- `CreatedAt`
- `IsRead` (bool)
- `UserId` (FK to ApplicationUser)

**Relationships:**
- N:1 → ApplicationUser

---

## Summary Diagram (ASCII)

```
┌──────────────────┐
│ ApplicationUser  │
│  (IdentityUser)  │
│ ─────────────────│
│ + InstructorId?  │───┐
│ + StudentId?     │─┐ │
└──────┬───────────┘ │ │
       │             │ │
       │             │ │
   ┌───▼────────┐    │ │
   │Notification│    │ │
   └────────────┘    │ │
                     │ │
       ┌─────────────▼─▼──────────┐
       │       Instructor          │
       │  ──────────────────────── │
       │  + DepartmentId (FK)      │
       │  + UserId (FK)            │
       └──────┬────────────────────┘
              │
              │ M:N (CourseInstructor)
              │
       ┌──────▼────────┐
       │    Course     │
       │  ────────────│
       │ + DepartmentId│
       └──┬────────────┘
          │
          │ 1:N
          │
   ┌──────▼──────────┐
   │   Enrollment    │
   │  ──────────────│
   │ + StudentId (FK)│
   │ + CourseId (FK) │
   │ + ProgressPercentage│
   │ + LastAccessDate    │
   │ + TotalHoursSpent   │
   └─────────────────┘

                  ┌──────────────┐
                  │   Student    │
                  │ ──────────── │
                  │ + UserId (FK)│
                  └──────────────┘

Course 1:N → CourseMaterial (UploadedBy: ApplicationUser)
Course 1:N → Assignment 1:N → AssignmentSubmission (Student)
Course 1:N → CourseAnnouncement (Author: ApplicationUser)
Course 1:N → Discussion 1:N → DiscussionPost (Author: ApplicationUser)
Enrollment 1:N → EnrollmentHistory (UpdatedBy: ApplicationUser)
```

---

## Key Design Decisions

1. **ApplicationUser dual-link**: InstructorId and StudentId are nullable FKs, allowing a user to be admin only, instructor only, student only, or theoretically both (future use case).
2. **Enrollment progress tracking**: Added ProgressPercentage, LastAccessDate, TotalHoursSpent for LMS analytics.
3. **Publishing workflow**: IsPublished flags on Assignment and CourseMaterial allow draft states before student visibility.
4. **Audit trail**: EnrollmentHistory tracks who changed enrollment status/grades and when.
5. **Restrict deletes**: UploadedBy and UpdatedBy relationships use Restrict to prevent orphaned audit records.
6. **Unique constraints**: 
   - (Course.Code, DepartmentId) ensures course codes unique within each department.
   - (Enrollment.StudentId, CourseId, Term) prevents duplicate enrollments.

---

## Migration History

- **Init** (20251104192431): Initial schema (Students, Courses, Departments, Enrollments, Instructors, CourseInstructor)
- **EnhancedModels**: Added Enrollment progress fields, Assignment/CourseMaterial metadata, EnrollmentHistory, CourseAnnouncement, Discussion, DiscussionPost, Notification

---

**For complete code implementation, see:**
- `Data/AppDbContext.cs` - EF Core configuration
- `Models/*.cs` - Entity definitions
- `Migrations/` - Database migrations
