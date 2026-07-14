## Project Vision

This project is a school management platform inspired by Lectio, but designed to be more flexible and reusable across different school systems.

The goal is not to build a system that only works for one specific school level. Instead, the system should be configurable enough to support different types of schools, such as primary schools, secondary schools, high schools, vocational schools, private schools, language schools, and potentially higher education later on.

The system should focus on the daily workflows of four main user groups:

* Students
* Teachers
* Parents/guardians
* School administrators/managers

The platform should provide a clear overview of classes, subjects, schedules, attendance, homework, grades, learning materials, and school communication.

A key design goal is that the system should not assume that every student in the same class has the exact same schedule. For example, two students in Class 1A may share most lessons, but one student may attend Spanish while another attends German. This means the system should support flexible subject groups, elective groups, language groups, and other custom lesson groups.

---

## User Roles

The system should support multiple user roles with different permissions.

### Student

Students should be able to:

* View their personal schedule
* View their subjects and teachers
* View classroom/location information for each lesson
* Download learning materials
* View homework and assignments
* Upload assignment submissions
* View their own grades
* View their own attendance
* View announcements relevant to them
* Receive notifications about schedule changes, homework deadlines, grades, and announcements

Students should not be able to view or edit other students' private information.

### Teacher

Teachers should be able to:

* View their teaching schedule
* View the students assigned to their classes/subjects
* Register attendance for each lesson
* Upload files and learning materials
* Create homework and assignments
* Set assignment deadlines
* Decide whether late submissions are blocked or allowed but marked as late
* View student submissions
* Grade student work
* Give written feedback
* Manage grades for their own subjects
* Create lesson notes or lesson plans
* Send announcements to specific classes, subject groups, or students

Teachers should only be able to manage the subjects, classes, and students they are responsible for.

### Parent/Guardian

Parents/guardians should have their own login, separate from the student account.

Parents should be able to:

* View their child’s schedule
* View their child’s subjects and teachers
* View their child’s attendance
* View their child’s grades, if enabled by the school
* View homework and deadlines
* View school announcements
* Receive notifications about important events, absences, missing assignments, or grade updates

A parent account should be able to be linked to more than one child.

Parents should only have access to their own child’s information.

### School Administrator / Manager

School administrators should be able to:

* Create and manage school years
* Create and manage terms/semesters
* Create and manage classes
* Create and manage subjects
* Create and manage rooms/classrooms
* Create and manage users
* Assign students to classes and subject groups
* Assign teachers to subjects/classes
* Create and manage schedules
* Handle schedule changes
* Handle substitute teachers
* Manage school-wide announcements
* Configure grading rules
* Configure attendance rules
* Export reports
* Manage permissions
* Archive previous school years

---

## Academic Structure

The system should support a configurable school structure.

Important entities may include:

* School
* Campus or building
* Classroom/room
* School year
* Term/semester
* Class/homeroom
* Subject
* Course/subject offering
* Student group
* Teacher assignment
* Student enrollment
* Lesson
* Schedule exception
* Assignment
* Submission
* Grade
* Attendance record
* Announcement
* File/material

The difference between a class and a subject group is important.

A class could be something like `Class 1A`, while a subject group could be `Spanish Beginner Group`, `German Group`, `Math Advanced`, or `Biology Lab Group`.

This allows students from the same class to have different subjects or lesson groups.

---

## Class Model vs Subject Group Model

The system should support both simple and advanced school structures.

Some schools may use a simple class-based model where students stay with the same classmates for most or all subjects during the school day. For example, Class 1A may have Mathematics, English, History, Science and Thai together as one fixed class group.

Other schools may use a more flexible model where students belong to a main class, but split into different subject groups for certain subjects such as language classes, electives, advanced courses, or special support classes.

Because of this, the system should not assume that a class and a subject group are always the same thing.

A `Class` should represent the student’s main administrative, homeroom, or base class.

A `SubjectGroup` or `CourseOffering` should represent the actual group of students attending a specific subject with a specific teacher during a specific term.

For simple schools, a `SubjectGroup` can include all students from a class.

Example:

```
Class: 1A

SubjectGroups:
- 1A - Mathematics
- 1A - English
- 1A - History
- 1A - Science

```

In this case, each `SubjectGroup` uses the same students from Class 1A.

For more advanced schools, some `SubjectGroups` may only include selected students from one or more classes.

Example:

```
Class: 1A

Shared subjects:
- 1A - Mathematics
- 1A - English
- 1A - History

Split subjects:
- Spanish Beginner Group
- German Beginner Group
- Chinese Beginner Group

```

This means two students can both belong to Class 1A, but still have different schedules because they are enrolled in different `SubjectGroups`.

The admin interface should make this simple. When creating a subject for a class, the administrator should be able to choose:

* Entire class
* Selected students
* Students from multiple classes
* Automatically enroll new students from the class
* Manually manage students in the subject group

This allows the system to support both simple class-based schools and more flexible high-school-style structures without needing two separate scheduling systems.

The important principle is:

```
Class = administrative group
SubjectGroup/CourseOffering = teaching group
Enrollment = connection between students and teaching groups
Lesson = actual scheduled class session
```

---

## Schedule / Timetable

The timetable is one of the core features of the system.

Each student and teacher should have a personalized schedule.

A scheduled lesson should contain:

* Subject
* Teacher
* Student group/class
* Classroom
* Start time
* End time
* Date
* Lesson status
* Attached homework or learning material
* Optional note from the teacher

The schedule should support:

* Weekly recurring lessons
* Half-year or full-year planning
* Term-based planning
* One-time schedule changes
* Cancelled lessons
* Moved lessons
* Room changes
* Substitute teachers
* Special events
* Exam periods
* Holidays
* Study days

The system should avoid hardcoding schedules week by week. Ideally, administrators should be able to create recurring timetable rules for a term or half-year, and then only adjust exceptions when needed.

Examples:

* Every Monday from 08:00 to 09:30, Class 1A has Math in Room 204 with Teacher A.
* Every Wednesday from 10:00 to 11:30, some students from Class 1A have Spanish while others have German.
* On one specific Friday, the Math lesson is cancelled.
* On one specific Tuesday, the English lesson is moved from Room 103 to Room 201.
* A substitute teacher replaces the normal teacher for one week.

The schedule should include conflict detection, such as:

* A teacher cannot teach two lessons at the same time
* A student cannot attend two lessons at the same time
* A classroom cannot be booked for two lessons at the same time
* A subject group should not overlap with another required subject group for the same students

---

## Attendance

Attendance should be registered for each lesson.

Teachers should be required to register attendance at the beginning of each class or lesson.

Attendance statuses could include:

* Present
* Absent
* Late
* Excused absence
* Sick
* Approved leave
* Not registered yet

Teachers should be able to:

* Register attendance for their own lessons
* Edit attendance within a limited time window
* Add notes to an attendance record
* Mark whether a student arrived late
* View attendance history for their own students/classes

Students should be able to:

* View their own attendance
* See absence percentage
* See absence per subject
* See absence over time

Parents should be able to:

* View their child’s attendance
* Receive notifications if their child is absent
* See attendance summaries and graphs

Administrators should be able to:

* View attendance across the school
* Generate attendance reports
* Configure attendance rules
* Correct attendance records if necessary
* Lock attendance records after a certain period

Attendance graphs could show:

* Total absence percentage
* Absence by subject
* Absence by month
* Late arrivals
* Excused vs unexcused absence

---

## Grades and Assessment

Teachers should be able to grade students in their own subjects.

The system should support different grade categories, such as:

* Homework
* Assignments
* Quizzes
* Midterms
* Final exams
* Oral exams
* Practical work
* Participation
* Final grade

Grades should be attached to:

* A student
* A subject/course
* A teacher
* A grading period
* An assignment or exam, if relevant

The system should support:

* Numeric grades
* Letter grades
* Pass/fail grades
* Custom grading scales
* Weighted grade categories
* Teacher comments
* Private draft grades
* Published grades visible to students/parents
* Final grade locking

Teachers should be able to save grades as drafts before publishing them.

Students should be able to view their own grades, but not edit them.

Parents may be able to view their child’s grades depending on school settings.

Administrators should be able to configure grading scales and generate grade reports.

---

## Assignments and Homework

Teachers should be able to create homework and assignments for specific classes or subject groups.

An assignment should include:

* Title
* Description
* Subject
* Teacher
* Assigned students/group
* Start date
* Deadline
* Attached files/materials
* Submission requirements
* Late submission policy
* Visibility status
* Optional grading settings

Students should be able to:

* View upcoming homework
* View deadlines
* Download attached files
* Upload their own submissions
* See whether their submission was submitted on time or late
* View feedback and grades after teacher review

Teachers should be able to:

* See all submissions
* Filter submitted, missing, late, and graded submissions
* Download student submissions
* Give feedback
* Grade submissions
* Allow or block resubmissions
* Extend deadlines for individual students if needed

Late submission rules should be configurable.

Possible rules:

* Submissions are blocked after the deadline
* Submissions are allowed after the deadline but marked as late
* Students can request an extension
* Teachers can manually reopen the assignment for specific students

The system should also support assignment states:

* Draft
* Published
* Closed
* Graded
* Archived

---

## File Uploads and Learning Materials

Teachers should be able to upload learning materials to their classes or subjects.

Files could include:

* PDF documents
* Word documents
* PowerPoints
* Images
* Videos
* Links
* Exercises
* Reading material

Files should be attachable to:

* A subject
* A class/group
* A specific lesson
* A specific assignment

Students should be able to download files that are relevant to their own subjects/classes.

The system should include file permissions, so students cannot access files for subjects or groups they are not enrolled in.

Possible file-related features:

* File size limits
* Allowed file types
* File version history
* File categories
* File descriptions
* Upload date
* Uploaded by
* Archive/delete options
* Virus scanning in a real production environment

---

## Communication and Announcements

The platform should support communication between the school and users.

Announcement types could include:

* School-wide announcements
* Class announcements
* Subject announcements
* Teacher-to-student announcements
* Teacher-to-parent announcements
* Administrator announcements

Announcements should include:

* Title
* Message
* Author
* Target audience
* Publish date
* Expiry date
* Attachments
* Read status

Users should only see announcements relevant to them.

Possible notification channels:

* In-app notifications
* Email notifications
* Push notifications
* Optional SMS or messaging-app integration later

Notification examples:

* Schedule changed
* Lesson cancelled
* Room changed
* New homework assigned
* Assignment deadline approaching
* Grade published
* Student marked absent
* New announcement published

---

## Lesson Planning

Teachers may need a way to plan individual lessons.

A lesson plan could include:

* Learning objectives
* Lesson description
* Materials
* Homework
* Internal teacher notes
* Substitute teacher notes
* Follow-up tasks

Lesson plans could be private to the teacher or shared with students, depending on the content.

This feature could be especially useful when a substitute teacher needs to take over a class.

---

## Substitute Teachers

The system should support substitute teachers.

A substitute teacher should be able to:

* View the lessons they are covering
* View the relevant class/group
* View substitute notes
* Register attendance for that lesson
* Access lesson materials if allowed

The normal teacher should still remain connected to the subject, but the substitute teacher should be temporarily assigned to specific lessons.

---

## Dashboards

Each user role should have a dashboard.

### Student Dashboard

The student dashboard should show:

* Today’s schedule
* Upcoming lessons
* Homework deadlines
* Missing assignments
* Recent grades
* Attendance summary
* Important announcements

### Teacher Dashboard

The teacher dashboard should show:

* Today’s teaching schedule
* Classes that need attendance registration
* Assignments waiting for grading
* Upcoming lessons
* Recent student submissions
* Announcements
* Schedule changes

### Parent Dashboard

The parent dashboard should show:

* Child’s schedule
* Attendance warnings
* Upcoming homework deadlines
* Recent grades
* Important school announcements

### Admin Dashboard

The admin dashboard should show:

* School overview
* Attendance statistics
* Missing attendance registrations
* Schedule conflicts
* Upcoming exams/events
* User management shortcuts
* Reports

---

## Reports and Exports

The system should support reporting.

Possible reports:

* Student attendance report
* Class attendance report
* Subject attendance report
* Grade report
* Missing assignments report
* Homework completion report
* Teacher schedule report
* Room usage report
* Parent contact report
* Student transcript/report card

Reports should be exportable as:

* PDF
* CSV
* Excel, if implemented later

Administrators should be able to generate reports for a selected:

* Student
* Class
* Subject
* Teacher
* Term
* School year

---

## Admin Configuration

The system should be configurable by the school.

School administrators should be able to configure:

* School name
* School year
* Terms/semesters
* Subjects
* Classes
* Rooms
* Teachers
* Students
* Parent accounts
* Grading scales
* Attendance rules
* Assignment rules
* Notification settings
* User permissions

The system should avoid hardcoded school-specific behavior.

For example, the system should not assume that all schools use the same grade scale, same school year structure, same class naming system, or same attendance rules.

---

## Authentication and Permissions

The system should include secure authentication and authorization.

Important requirements:

* Users must log in with their own account
* Passwords must be securely hashed
* Roles must control access
* Students can only view their own data
* Parents can only view their own child’s data
* Teachers can only manage their own classes/subjects
* Administrators can manage school-level data
* Sensitive actions should be logged

Possible future features:

* Two-factor authentication
* Login with Google or Microsoft
* Password reset flow
* Account invitation emails
* Session management
* Audit logs

---

## Audit Logs and History

Important changes should be logged.

Examples of actions that should be logged:

* A grade was changed
* An attendance record was edited
* A schedule was changed
* A lesson was cancelled
* A file was uploaded
* A user role was changed
* A student was moved to another class/group

Audit logs are important because schools handle sensitive student data.

The system should make it possible to see who changed what and when.

---

## Localization and Thai Language Support

The system should support localization because the primary target market is Thailand, while still keeping English as a supported language.

The platform should support at least:

* English
* Thai

The frontend should avoid hardcoded UI text. All labels, buttons, messages, validation errors, menu items, notification messages, and status texts should come from translation files or a localization system.

Examples of UI text that should be translatable:

* Login
* Schedule
* Attendance
* Assignments
* Grades
* Submit homework
* Download file
* Present
* Absent
* Late
* Excused absence
* Deadline
* Published
* Draft
* Cancelled lesson
* Room changed
* Grade published

The system should separate system-generated text from user-generated content.

System-generated text should be translated by the application.

Examples:

```txt
"Assignment submitted successfully"
"Your lesson has been cancelled"
"Attendance has not been registered yet"
```

User-generated content should normally stay in the language it was written in.

Examples:

```txt
Teacher-written assignment descriptions
Uploaded file names
Lesson notes
Teacher feedback
Announcements written by the school
```

This means a Thai school can write announcements, homework, feedback, and lesson material in Thai, while an international school could write the same content in English.

---

## Language Preference

Each user should be able to choose their preferred language.

Possible language settings:

* English
* Thai
* School default language

The school should also have a default language setting.

Example:

```txt
School default language: Thai
Student language: Thai
Teacher language: English
Parent language: Thai
```

The system should remember the user’s language preference after login.

---

## Thai Text Support

The system must fully support Thai text in both frontend and backend.

This includes:

* Thai names
* Thai subject names
* Thai classroom names
* Thai announcements
* Thai assignment descriptions
* Thai file names
* Thai teacher feedback
* Thai parent information

The database should use proper Unicode-compatible text fields.

For example, the system should not assume that all names or text values only use English letters.

Search should also support Thai text, so users can search for students, teachers, subjects, rooms, files, and announcements using Thai characters.

---

## Thai Names and Titles

The system should not assume that all users follow Western naming conventions.

Thai users may have:

* First name
* Last name
* Nickname
* Thai honorific/title
* English display name, if relevant

Possible titles/honorifics could include:

* Mr.
* Mrs.
* Miss
* Master
* นาย
* นาง
* นางสาว
* เด็กชาย
* เด็กหญิง
* คุณ
* ครู
* อาจารย์

The system should support a flexible display name.

Example:

```txt
Legal name: Somchai Srisuk
Thai name: สมชาย ศรีสุข
Nickname: Chai
Display name: ครูสมชาย
```

For students, it may also be useful to support nicknames because Thai students often use nicknames in daily school life.

---

## Date, Time and Calendar Formatting

The system should support Thai-friendly date and time formatting.

The backend should store dates and times in a consistent technical format, but the frontend should display them based on the user’s language and locale.

The system should consider:

* Thai date format
* English date format
* 24-hour time format
* Thailand timezone
* School year and term naming
* Possible Buddhist Era year display

Example:

```txt
English display:
Monday, 12 August 2026

Thai display:
วันจันทร์ที่ 12 สิงหาคม 2569
```

Internally, the system should still store dates in a reliable standard format to avoid confusion.

The system should not hardcode the Danish, European, American, or Thai date format directly into the database logic.

---

## School Terminology

School terminology should be configurable because different schools may use different words.

For example, the system should not hardcode one specific school structure.

Possible English terms:

* Class
* Homeroom
* Subject
* Course
* Term
* Semester
* Grade
* Attendance
* Assignment
* Final exam

Possible Thai terms:

* ห้องเรียน
* ชั้นเรียน
* วิชา
* ภาคเรียน
* ปีการศึกษา
* คะแนน
* การเข้าเรียน
* การบ้าน
* งานที่มอบหมาย
* สอบกลางภาค
* สอบปลายภาค

The system should allow Thai schools to use terms that make sense for their own school structure.

For example, some Thai schools may use terms like:

```txt
ป.1, ป.2, ป.3
ม.1, ม.2, ม.3
ม.4, ม.5, ม.6
```

The system should not assume that class names are always like `Class 1A`.

---

## Thai and English Subject Names

Subjects should support both Thai and English names.

Example:

```txt
Subject internal ID: math
English name: Mathematics
Thai name: คณิตศาสตร์
```

Another example:

```txt
Subject internal ID: thai-language
English name: Thai Language
Thai name: ภาษาไทย
```

This makes it possible for the same system to show subjects in English for some users and Thai for others.

The same principle could be used for:

* Subjects
* Rooms
* Grade categories
* Attendance statuses
* Assignment states
* School terms
* Notification types

---

## Fonts and UI Design for Thai

The frontend should use fonts that support Thai characters properly.

Thai text should be readable on both desktop and mobile devices.

Important UI considerations:

* Thai text can take up more or less horizontal space than English text
* Buttons should not rely on very short text labels
* Table columns should have enough space for Thai names
* Long Thai names should not break the layout
* Mobile views should be tested with Thai text
* Form validation messages should be readable in Thai
* PDF exports should support Thai fonts

The UI should be tested in both English and Thai.

---

## Notifications in Multiple Languages

Notifications should be generated in the user’s preferred language when possible.

Example English notification:

```txt
Your Mathematics lesson has been cancelled.
```

Example Thai notification:

```txt
คาบเรียนคณิตศาสตร์ของคุณถูกยกเลิก
```

Parents should receive notifications in their own preferred language, not necessarily the student’s language.

For example:

```txt
Student language: English
Parent language: Thai
Teacher language: English
```

---

## Reports and PDF Export in Thai

Reports and exports should support Thai text.

This includes:

* Attendance reports
* Grade reports
* Student transcripts
* Assignment reports
* Parent reports
* PDF exports
* CSV exports
* Excel exports, if implemented later

PDF generation must support Thai fonts, otherwise Thai text may appear broken, missing, or unreadable.

---

## Technical Localization Principle

Localization should be considered from the beginning of the project.

The system should avoid:

* Hardcoded frontend text
* Hardcoded date formats
* Hardcoded grade labels
* Hardcoded attendance labels
* Hardcoded school level names
* English-only database fields
* English-only search
* UI layouts that only work with English text

The system should be designed so that adding more languages later is possible without rewriting the entire frontend or backend.

The first supported languages should be English and Thai.

---

## Accessibility and Mobile Use

The platform should be usable on both desktop and mobile devices.

Many students and parents may primarily use phones, so the frontend should be responsive and mobile-friendly.

Important UI considerations:

* Clear schedule overview
* Simple navigation
* Large touch-friendly buttons
* Readable text
* Accessible colors
* Keyboard navigation where relevant
* Good contrast
* Simple parent/student dashboards

---

## Important Edge Cases

The system should support realistic school situations, such as:

* A student changes class during the school year
* A student joins a subject group late
* A student leaves the school
* A teacher is replaced
* A teacher is absent and a substitute teacher takes over
* A classroom becomes unavailable
* A lesson is cancelled
* A lesson is moved to another room
* A student has two parents/guardians with separate accounts
* A parent has multiple children at the same school
* A grade is saved as a draft before being published
* An assignment deadline is extended for one specific student
* A student submits homework late
* A student is absent from an exam
* Attendance is registered incorrectly and needs correction
* A school year needs to be archived without deleting old data

---

## Possible MVP Features

The first version of the system should focus on the most important features instead of trying to implement everything at once.

A realistic MVP could include:

1. User login and role-based access
2. Student, teacher, parent, and admin roles
3. Class and subject management
4. Student enrollment in classes/subject groups
5. Teacher assignment to subjects/classes
6. Basic timetable/schedule overview
7. Classroom/location information
8. Attendance registration
9. Student/parent attendance overview
10. Assignment creation
11. Student assignment submission
12. Teacher grading/feedback
13. Basic grade overview
14. File upload/download for learning materials
15. Announcements
16. Admin management panel

---

## Possible Future Features

Future versions could include:

* Calendar export
* Push notifications
* Email notifications
* Mobile app
* Advanced report generation
* Exam planning
* Seating plans
* Behavior/discipline notes
* Parent-teacher meeting booking
* AI-assisted lesson planning
* Integration with Google/Microsoft accounts
* Import students from CSV/Excel
* Export report cards as PDF
* Multi-school support
* Multi-campus support
* Advanced permission system
* Timetable conflict detection
* Automatic timetable generation
* Messaging between teachers and parents
* Student progress analytics
* Learning goals and curriculum mapping

---

## Core Design Principle

The system should be flexible, role-based, and centered around the real daily workflows of schools.

The most important concept is that schedules, subjects, grades, assignments, attendance, files, and communication should all be connected to the correct students, teachers, classes, groups, and school terms.

The system should not assume that all students in the same class always have the exact same subjects, rooms, teachers, or schedules.

Flexibility is the key difference between a simple school schedule app and a real school management platform.
