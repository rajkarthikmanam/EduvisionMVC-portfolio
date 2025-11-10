# Login Credentials for EduVision LMS

## Admin Account
- **Email:** admin@local.test
- **Password:** Admin123!
- **Role:** Admin
- **Access:** Full system administration

---

## Instructor Accounts

### 1. Computer Science Instructor (Demo Instructor)
- **Email:** instructor@local.test
- **Password:** Instructor123!
- **Department:** Computer Science
- **Courses:** CS101, CS201, CS250, CS310

### 2. Mathematics Instructor (Sarah Johnson)
- **Email:** math.prof@local.test
- **Password:** Math123!
- **Department:** Mathematics
- **Courses:** MATH201 (Calculus II), MATH301 (Linear Algebra)

### 3. Engineering Instructor (Michael Chen)
- **Email:** eng.prof@local.test
- **Password:** Eng123!
- **Department:** Engineering
- **Courses:** ENG101 (Introduction to Engineering), ENG250 (Thermodynamics)

### 4. Business Instructor (Amanda Rodriguez)
- **Email:** bus.prof@local.test
- **Password:** Bus123!
- **Department:** Business Administration
- **Courses:** BUS101 (Business Fundamentals), BUS210 (Financial Accounting)

---

## Student Account
- **Email:** student@local.test
- **Password:** Student123!
- **Role:** Student
- **Enrolled Courses:** 3 courses across different departments

---

## Usage Instructions

1. Navigate to **http://localhost:5000**
2. Click **Login** in the top navigation
3. Enter one of the email addresses and passwords above
4. You'll be redirected to your role-specific dashboard:
   - **Admin:** Admin Dashboard
   - **Instructor:** Instructor Dashboard (shows only your department's courses)
   - **Student:** Student Dashboard (shows enrolled courses with progress tracking)

## Testing Multi-Department Features

To see how different instructors access different courses:

1. Login as **instructor@local.test** → See Computer Science courses
2. Logout → Login as **math.prof@local.test** → See Mathematics courses
3. Logout → Login as **eng.prof@local.test** → See Engineering courses
4. Logout → Login as **bus.prof@local.test** → See Business courses

Each instructor can only manage courses in their assigned department.
