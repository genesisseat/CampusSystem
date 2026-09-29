# Faculty Portal Database Tables

This reference documents the shared MySQL tables queried and updated by the Faculty Portal.

## Database connections

- Connection name: `DefaultConnection`, targeting shared MySQL database `mydb`.
- Provide `ConnectionStrings__DefaultConnection` through the environment or a secret manager; do not commit credentials.
- Student references use integer `user.id`.

## Shared MySQL tables used by Faculty Portal

### `class_offerings`

| Column | Use |
|---|---|
| id | Offering identifier |
| subject_id | Reference to `subjects.id` |
| section_code | Section identifier |
| instructor_name | Assigned instructor |
| room | Classroom |
| days_of_week | Meeting days |
| start_time, end_time | Meeting times |
| school_year, semester | Academic term |

### `subjects`

| Column | Use |
|---|---|
| id | Subject identifier |
| subject_code | Course code |
| subject_name | Subject title |
| units | Credit units |

### `enrolled_subjects`

| Column | Use |
|---|---|
| id | Enrolled-subject identifier |
| enrollment_id | Reference to `enrollments.id` |
| class_offering_id | Reference to `class_offerings.id` |

### `enrollments`

| Column | Use |
|---|---|
| id | Enrollment identifier |
| student_id | Student user identifier |

### `user`

| Column | Use |
|---|---|
| id | User/student identifier |
| student_id_number | Student number |
| name | Student name |
| role | Used to select student users |
| status | Used to select active student users |

### `student_profile`

| Column | Use |
|---|---|
| user_id | Reference to `user.id` |
| program | Student program |
| year_level | Student year level |

### `grades`

| Column | Use |
|---|---|
| enrolled_subject_id | Related enrolled subject; used for upsert |
| grade | Final grade |
| is_inc | Incomplete flag |
| remarks | Grade comments |
| status | Grade state |
| graded_at | Timestamp set when saving a grade |

### `student_clearance`

| Column | Use |
|---|---|
| student_id | Student user identifier |
| department_name | Used to select Finance clearance |
| status | Clearance state |
| id | Used to choose the most recent row |

## Relationships

- `class_offerings.subject_id` -> `subjects.id`
- `enrolled_subjects.enrollment_id` -> `enrollments.id`
- `enrolled_subjects.class_offering_id` -> `class_offerings.id`
- `grades.enrolled_subject_id` -> `enrolled_subjects.id`
- `student_profile.user_id` and `enrollments.student_id` refer to the MySQL `user` table.

## Notes

- These MySQL tables are shared with Student Portal, Finance, and Registrar workflows; they are not unique Faculty-owned tables.
- MySQL table definitions are not declared in this project. The columns above are those read or written by `FacultyDbService`, not a complete schema dump.
- `FacultyDbService` reads rosters and offerings and writes grades; assignment and attendance workflows remain incomplete.
