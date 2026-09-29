# Faculty Portal Database Tables

This reference separates the Faculty Portal's shared MySQL tables from its SQL Server guidance/authentication persistence. The table and column descriptions are based on queries and EF Core mappings in the current project; MySQL column types are only stated where code establishes them.

## Database connections

- MySQL `DefaultConnection`: host `100.98.41.69`, port `3306`, database `mydb`.
- SQL Server `CampusSystemDb`: `localhost,1433`, database `CampusSystemDb`.
- Faculty roster, class and grade operations use MySQL. Guidance requests and refresh tokens use SQL Server.

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

## SQL Server shared persistence tables

### `dbo.Students`

| Column | SQL Server type | Notes |
|---|---|---|
| Id | uniqueidentifier | Primary key |
| StudentNumber | nvarchar(max) | Shared student number |
| FullName | nvarchar(max) | Student name |
| Email | nvarchar(max) | Email address |

### `guidance.GuidanceRequests`

| Column | SQL Server type | Notes |
|---|---|---|
| Id | uniqueidentifier | Primary key |
| StudentId | uniqueidentifier | Student reference |
| Subject | nvarchar(max) | Request subject |
| Details | nvarchar(max) | Request details |
| SafetyValveText | nvarchar(max), nullable | Optional safety text |
| Urgency | int | Enum value |
| Status | int | Enum value |
| AssignedCounselorId | uniqueidentifier, nullable | Optional counselor |
| IdempotencyKey | nvarchar(max), nullable | Duplicate-submission key |
| RowVersion | varbinary(max) | Concurrency value |

### `guidance.RefreshTokens`

| Column | SQL Server type | Notes |
|---|---|---|
| Token | nvarchar(450) | Primary key |
| Subject | nvarchar(max) | Token subject |
| ExpiresAt | datetimeoffset | Expiration time |

## Relationships

- `class_offerings.subject_id` -> `subjects.id`
- `enrolled_subjects.enrollment_id` -> `enrollments.id`
- `enrolled_subjects.class_offering_id` -> `class_offerings.id`
- `grades.enrolled_subject_id` -> `enrolled_subjects.id`
- `student_profile.user_id` and `enrollments.student_id` refer to the MySQL `user` table.
- `guidance.GuidanceRequests.StudentId` corresponds to the shared student identity; no EF foreign key is configured.

## Notes

- The MySQL tables are shared with Student Portal, Finance, and Registrar workflows; they are not unique Faculty-owned tables.
- MySQL table definitions are not declared in this project. The columns above are those read or written by `FacultyDbService`, not a complete schema dump.
- SQL Server entity mappings are in `Data/GuidanceDbContext.cs`.
