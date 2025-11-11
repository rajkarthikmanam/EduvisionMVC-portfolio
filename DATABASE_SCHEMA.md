# 🎓 EduVision MVC - Database Schema Documentation

## Overview
This document describes the complete database schema for the EduVision Learning Management System built with ASP.NET Core MVC and Entity Framework Core.

---

## 📊 Core Entities (5 Tables)

### 1. **STUDENT**
Primary academic user entity representing enrolled students.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | int | PK | Primary key |
| `UserId` | string | FK → ApplicationUser | Link to Identity user |
| `Name` | string(120) | Required | Full name |
| `Email` | string | Required, Unique | Email address |
| `Major` | string(50) | Required | Major field of study |
| `Age` | int | | Student age |
| `GPA` | decimal(3,2) | | Grade Point Average |
| `DepartmentId` | int | FK → Department | Home department |
| `AdvisorInstructorId` | int | FK → Instructor | Academic advisor |
| `Phone` | string | | Contact number |
| `AcademicLevel` | string(40) | | Freshman/Sophomore/Junior/Senior |
| `TotalCredits` | int | Required | Target graduation credits |
| `EnrollmentDate` | datetime | | Initial enrollment date |

**Computed Properties:**
- `CreditsInProgress`: Sum of credits from approved enrollments
- `CompletedCredits`: Sum of credits from completed enrollments with grades

---

### 2. **COURSE**
Represents academic courses offered by departments.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | int | PK | Primary key |
| `Code` | string(20) | Required, Unique per Dept | Course code (e.g., CS101) |
| `Title` | string(120) | Required | Course title |
| `Description` | string(1000) | | Course description |
| `Credits` | int | Range 0-10 | Credit hours |
| `Capacity` | int | Range 1-500 | Max enrollment |
| `RequiresApproval` | bool | | Enrollment requires instructor approval |
| `Level` | string(50) | | Introductory/Intermediate/Advanced |
| `DeliveryMode` | string(50) | | Online/In-Person/Hybrid |
| `Prerequisites` | string(200) | | Required prior courses |
| `DepartmentId` | int | FK → Department, Required | Offering department |
| `Schedule` | string(100) | | Meeting times |
| `StartDate` | datetime | | Course start date |
| `EndDate` | datetime | | Course end date |
| `Location` | string(200) | | Physical/virtual location |

**Computed Properties:**
- `CurrentEnrollments`: Count of approved/completed enrollments
- `IsFull`: Boolean if enrollment >= capacity
- `AverageGrade`: Mean of all numeric grades

---

### 3. **ENROLLMENT**
Junction entity with rich attributes linking students to courses.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | int | PK | Primary key |
| `StudentId` | int | FK → Student, Required | Enrolled student |
| `CourseId` | int | FK → Course, Required | Enrolled course |
| `Term` | string | Required | Academic term (e.g., Fall 2025) |
| `Status` | enum | | Pending/Approved/Completed/Dropped/Withdrawn |
| `Numeric_Grade` | decimal | | Grade point (0.0-4.0) |
| `LetterGrade` | string | | Calculated letter grade (A-F) |
| `IsRepeatAttempt` | bool | | Whether student is retaking |
| `AttemptNumber` | int | Default 1 | Attempt sequence number |
| `ProgressPercentage` | decimal | Default 0 | Course completion % |
| `EnrolledDate` | datetime | | Initial enrollment timestamp |
| `ApprovedDate` | datetime | | Instructor approval timestamp |
| `DroppedDate` | datetime | | Drop timestamp |
| `CompletedDate` | datetime | | Completion timestamp |
| `LastAccessDate` | datetime | | Last student access |
| `TotalHoursSpent` | int | Default 0 | Logged study hours |
| `Notes` | string | | Additional notes |

**Unique Constraint:** `(StudentId, CourseId, Term)`

**Computed Properties:**
- `IsActive`: Approved status without grade
- `IsCompleted`: Completed status with grade

---

### 4. **DEPARTMENT**
Academic departments organizing courses and faculty.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | int | PK | Primary key |
| `Code` | string(10) | Required, Unique | Department code (e.g., CS) |
| `Name` | string(120) | Required | Department name |
| `Description` | string(800) | | Department description |
| `OfficeLocation` | string(100) | | Physical office location |
| `Phone` | string | | Contact phone |
| `Email` | string | | Contact email |
| `Website` | string | | Department website URL |
| `ChairId` | int | FK → Instructor | Department chair |

**Relationships:**
- One-to-Many: Department → Courses
- One-to-Many: Department → Students
- One-to-Many: Department → Instructors
- One-to-One: Department → Chair (self-referencing via Instructor)

---

### 5. **INSTRUCTOR**
Faculty members teaching courses and advising students.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | int | PK | Primary key |
| `UserId` | string | FK → ApplicationUser | Link to Identity user |
| `FirstName` | string(50) | Required | First name |
| `LastName` | string(50) | Required | Last name |
| `Email` | string | Required, Unique | Email address |
| `Title` | string | | Academic title (e.g., Professor) |
| `OfficeLocation` | string | | Office location |
| `Phone` | string | | Contact phone |
| `OfficeHours` | string | | Office hours schedule |
| `HireDate` | datetime | | Employment start date |
| `Bio` | string | | Biography/research interests |
| `DepartmentId` | int | FK → Department, Required | Home department |

**Computed Properties:**
- `DisplayName`: FirstName + LastName

---

## 🔗 Join Tables

### **COURSE_INSTRUCTOR**
Many-to-many relationship between courses and instructors.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `CourseId` | int | PK, FK → Course | Course being taught |
| `InstructorId` | int | PK, FK → Instructor | Instructor teaching |

**Composite Primary Key:** `(CourseId, InstructorId)`

---

## 🔐 Identity & User Management

### **APPLICATION_USER** (extends IdentityUser)
ASP.NET Core Identity user with custom properties.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | string | PK | Identity GUID |
| `UserName` | string | Required, Unique | Login username |
| `Email` | string | Required, Unique | Email address |
| `FirstName` | string | | User first name |
| `LastName` | string | | User last name |
| `CreatedAt` | datetime | Default NOW | Account creation |
| `LastLoginDate` | datetime | | Last login timestamp |
| `IsActive` | bool | Default true | Account status |
| `StudentId` | int | FK → Student | Link to student record |
| `InstructorId` | int | FK → Instructor | Link to instructor record |
| `Avatar` | string | | Profile picture URL |

**Roles:** Admin, Instructor, Student

---

## 📚 Extended LMS Features

### **ASSIGNMENT**
Course assignments/homework.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | int | PK |
| `CourseId` | int | FK → Course |
| `Title` | string | Assignment title |
| `Description` | string | Instructions |
| `MaxPoints` | int | Maximum score |
| `DueDate` | datetime | Submission deadline |
| `AvailableFrom` | datetime | Availability start |
| `IsPublished` | bool | Visibility status |

---

### **ASSIGNMENT_SUBMISSION**
Student submissions for assignments.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | int | PK |
| `AssignmentId` | int | FK → Assignment |
| `StudentId` | int | FK → Student |
| `SubmissionText` | string | Text submission |
| `FilePath` | string | Uploaded file path |
| `SubmittedAt` | datetime | Submission timestamp |
| `Grade` | decimal | Assigned grade |
| `Feedback` | string | Instructor feedback |

---

### **COURSE_MATERIAL**
Learning resources uploaded to courses.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | int | PK |
| `CourseId` | int | FK → Course |
| `UploadedById` | string | FK → ApplicationUser |
| `Title` | string | Resource title |
| `Description` | string | Resource description |
| `FilePath` | string | File location |
| `FileType` | string | MIME type |
| `UploadedAt` | datetime | Upload timestamp |

---

### **DISCUSSION**
Course discussion forums.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | int | PK |
| `CourseId` | int | FK → Course |
| `Topic` | string | Discussion topic |
| `Description` | string | Topic description |
| `CreatedAt` | datetime | Creation timestamp |
| `IsLocked` | bool | Whether locked |

---

### **DISCUSSION_POST**
Individual posts in discussions.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | int | PK |
| `DiscussionId` | int | FK → Discussion |
| `AuthorId` | string | FK → ApplicationUser |
| `Content` | string | Post content |
| `PostedAt` | datetime | Post timestamp |
| `Likes` | int | Like count |

---

### **COURSE_ANNOUNCEMENT**
Instructor announcements for courses.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | int | PK |
| `CourseId` | int | FK → Course |
| `AuthorId` | string | FK → ApplicationUser |
| `Title` | string | Announcement title |
| `Content` | string | Announcement body |
| `PublishedAt` | datetime | Publication timestamp |
| `IsPinned` | bool | Pin to top |

---

## 📝 Audit & Tracking

### **ENROLLMENT_HISTORY**
Audit trail for enrollment changes.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | int | PK |
| `EnrollmentId` | int | FK → Enrollment |
| `UpdatedById` | string | FK → ApplicationUser |
| `OldStatus` | string | Previous status |
| `NewStatus` | string | New status |
| `OldGrade` | decimal | Previous grade |
| `NewGrade` | decimal | New grade |
| `ChangedAt` | datetime | Change timestamp |
| `Notes` | string | Change notes |

---

### **NOTIFICATION**
User notifications system.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | int | PK |
| `UserId` | string | FK → ApplicationUser |
| `Title` | string | Notification title |
| `Message` | string | Notification content |
| `CreatedAt` | datetime | Creation timestamp |
| `IsRead` | bool | Read status |
| `NotificationType` | string | Type classification |

---

### **USER_PROFILE**
Extended user profile data.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | int | PK |
| `UserId` | string | FK → ApplicationUser |
| `Biography` | string | User bio |
| `Preferences` | string | JSON preferences |
| `Theme` | string | UI theme choice |

---

## 🔑 Key Relationships Summary

```
Department (1) ──→ (Many) Course
Department (1) ──→ (Many) Student
Department (1) ──→ (Many) Instructor
Department (1) ──→ (1) Instructor [Chair]

Student (Many) ←──→ (Many) Course [via Enrollment]
Course (Many) ←──→ (Many) Instructor [via CourseInstructor]

Student (Many) ──→ (1) Instructor [Advisor]
Student (Many) ──→ (1) Department
Instructor (Many) ──→ (1) Department

ApplicationUser (1) ──→ (1) Student
ApplicationUser (1) ──→ (1) Instructor

Course (1) ──→ (Many) Assignment
Assignment (1) ──→ (Many) AssignmentSubmission
Student (1) ──→ (Many) AssignmentSubmission

Course (1) ──→ (Many) Discussion
Discussion (1) ──→ (Many) DiscussionPost

Enrollment (1) ──→ (Many) EnrollmentHistory
```

---

## 🛠️ Technical Implementation

**ORM:** Entity Framework Core 9.0  
**Database:** SQLite (Development), SQL Server/Azure SQL (Production)  
**Migration Count:** 7 migrations  
**Indexing:**
- Unique: Student.Email, Instructor.Email, Department.Code
- Composite Unique: Course(Code, DepartmentId), Enrollment(StudentId, CourseId, Term)

**Cascade Behaviors:**
- Enrollment → Student: Cascade Delete
- Enrollment → Course: Cascade Delete
- Student → Department: Set Null
- Others: Restrict/Set Null for data integrity

---

## 📊 Statistics

- **Total Tables:** 14 (+ 7 Identity tables)
- **Total Relationships:** 25+
- **Enumerations:** 1 (EnrollmentStatus)
- **Computed Properties:** 8+
- **Unique Constraints:** 5
- **Foreign Keys:** 22+

---

*Generated for EduVision MVC - ASP.NET Core Learning Management System*  
*Last Updated: November 11, 2025*
