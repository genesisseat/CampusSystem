# Student Department Database Tables

This document summarizes the database tables used by the Student Portal department, based on the application code and MySQL queries in the Student Portal project.

> All operational departments use shared MySQL database `mydb` through `ConnectionStrings:DefaultConnection`. Student identity is the integer `user.id`.

## 1) Core student and identity tables

### `user`

| Column | Type | Notes |
|---|---|---|
| id | int | Primary key |
| student_id_number | varchar | Student number |
| name | varchar | Full name |
| email | varchar | Email |
| role | varchar | e.g. `student` |
| status | varchar | e.g. `active` |
| created_at | datetime | Created timestamp |

### `student_profile`

| Column | Type | Notes |
|---|---|---|
| user_id | int | Student user reference |
| program | varchar | Program name |
| year_level | varchar | Year level |
| curriculum_year | varchar | Curriculum period |
| average_grade | decimal | GPA |
| academic_status | varchar | Standing |
| enrollment_status | varchar | Enrollment state |
| completed_units | int | Completed credits |
| units_remaining | int | Remaining credits |
| guidance_clearance_status | varchar | Guidance clearance field |
| guidance_notes | text | Notes |
| contact_number | varchar | Mobile/contact |
| address | text | Address |
| birth_date | datetime | DOB |
| gender | varchar | Gender |

### `settings`

| Column | Type | Notes |
|---|---|---|
| key | varchar | Setting key |
| value | varchar | Setting value |

### `activity_log`

| Column | Type | Notes |
|---|---|---|
| id | int | Primary key |
| user_id | int | Related user |
| message | text | Activity note |
| created_at | datetime | Timestamp |

## 2) Academic tables

### `subjects`

| Column | Type | Notes |
|---|---|---|
| id | int | Primary key |
| subject_code | varchar | Subject code |
| subject_name | varchar | Subject title |
| units | int | Units |
| lec_hours | int | Lecture hours |
| lab_hours | int | Lab hours |

### `class_offerings`

| Column | Type | Notes |
|---|---|---|
| id | int | Primary key |
| subject_id | int | FK to `subjects.id` |
| section_code | varchar | Section code |
| instructor_name | varchar | Instructor |
| room | varchar | Room |
| days_of_week | varchar | Schedule days |
| start_time | time | Start time |
| end_time | time | End time |
| school_year | varchar | Academic year |
| semester | varchar | Semester |
| status | varchar | Offering status |

### `enrollments`

| Column | Type | Notes |
|---|---|---|
| id | int | Primary key |
| student_id | int | Student reference |
| school_year | varchar | Academic year |
| semester | varchar | Semester |
| status | varchar | Enrollment status |

### `enrolled_subjects`

| Column | Type | Notes |
|---|---|---|
| id | int | Primary key |
| enrollment_id | int | FK to `enrollments.id` |
| class_offering_id | int | FK to `class_offerings.id` |
| status | varchar | e.g. `enrolled` |

### `grades`

| Column | Type | Notes |
|---|---|---|
| id | int | Primary key |
| enrolled_subject_id | int | FK to `enrolled_subjects.id` |
| grade | decimal | Grade value |
| is_inc | tinyint | Incomplete flag |
| status | varchar | Grade status |
| remarks | text | Grade remarks |

## 3) Student request / petition tables

### `document_requests`

| Column | Type | Notes |
|---|---|---|
| id | int | Primary key |
| student_id | int | Student reference |
| document_type | varchar | Requested document |
| purpose | text | Request purpose |
| copies | int | Requested copies |
| status | varchar | pending / processing / ready |
| qr_code_token | varchar | QR code token |
| remarks | text | Notes |
| requested_at | datetime | Request time |
| processed_at | datetime | Processing timestamp |
| released_at | datetime | Release timestamp |

### `add_drop_requests`

| Column | Type | Notes |
|---|---|---|
| id | int | Primary key |
| student_id | int | Student reference |
| enrollment_id | int | Related enrollment |
| request_type | varchar | add / drop / change |
| class_offering_id | int | Requested class |
| subject_code | varchar | Subject code |
| section_code | varchar | Section |
| target_class_offering_id | int | Replacement class |
| target_subject_code | varchar | Target subject |
| target_section_code | varchar | Target section |
| reason | text | Reason |
| status | varchar | pending / approved / rejected |
| created_at | datetime | Timestamp |

### `overload_requests`

| Column | Type | Notes |
|---|---|---|
| id | int | Primary key |
| student_id | int | Student reference |
| school_year | varchar | Academic year |
| semester | varchar | Semester |
| request_type | varchar | overload |
| requested_units | int | Requested unit load |
| reason | text | Reason |
| status | varchar | pending / approved / rejected |
| requested_at | datetime | Request timestamp |

### `completion_revision_requests`

| Column | Type | Notes |
|---|---|---|
| id | int | Primary key |
| student_id | int | Student reference |
| grade_id | int | Related grade |
| subject_code | varchar | Subject code |
| subject_name | varchar | Subject name |
| request_type | varchar | completion / revision |
| requested_grade | decimal | Requested grade |
| reason | text | Reason |
| status | varchar | pending / approved / rejected |
| requested_at | datetime | Request timestamp |

### `course_shifting_requests`

| Column | Type | Notes |
|---|---|---|
| id | int | Primary key |
| student_id | int | Student reference |
| from_program | varchar | Current program |
| to_program | varchar | Target program |
| reason | text | Reason |
| status | varchar | pending / approved / rejected |
| requested_at | datetime | Request timestamp |
| approved_at | datetime | Approval date |
| evaluated_by | varchar | Evaluator |

## 4) Clearance, finance, library, scholarship, assignment data

### `student_clearance`

| Column | Type | Notes |
|---|---|---|
| id | int | Primary key |
| student_id | int | Student reference |
| department_name | varchar | Department name |
| school_year | varchar | Academic year |
| semester | varchar | Semester |
| status | varchar | Cleared / Pending / Hold |
| cleared_by | varchar | Officer name |
| cleared_at | datetime | Clearance date |
| remarks | text | Notes |

### `student_payments`

| Column | Type | Notes |
|---|---|---|
| id | int | Primary key |
| student_id | int | Student reference |
| receipt_no | varchar | Receipt number |
| amount | decimal | Amount |
| pay_date | datetime | Payment date |
| payment_method | varchar | Payment method |
| status | varchar | Paid / Pending |
| description | varchar | Payment description |

### `library_reservations`

| Column | Type | Notes |
|---|---|---|
| id | int | Primary key |
| student_id | int | Student reference |
| book_title | varchar | Book title |
| book_author | varchar | Book author |
| reservation_date | datetime | Reservation date |
| due_date | datetime | Due date |
| status | varchar | Active / Returned / Overdue |

### `scholarship_applications`

| Column | Type | Notes |
|---|---|---|
| id | int | Primary key |
| student_id | int | Student reference |
| scholarship_name | varchar | Scholarship name |
| school_year | varchar | Academic year |
| semester | varchar | Semester |
| requirements_submitted | tinyint | Submitted flag |
| status | varchar | Status |
| requested_at | datetime | Request timestamp |

### `student_assignments`

| Column | Type | Notes |
|---|---|---|
| id | int | Primary key |
| student_id | int | Student reference |
| subject_code | varchar | Subject code |
| subject_name | varchar | Subject title |
| title | varchar | Assignment title |
| description | text | Description |
| max_score | decimal | Max score |
| due_date | datetime | Due date |
| status | varchar | Assigned state |
| is_submitted | tinyint | Submitted flag |
| my_score | decimal | Student score |

## 5) Guidance-related tables used by Student Portal

The Student Portal also consumes shared guidance persistence classes.

### `GuidanceRequests` (likely table name)

| Column | Type | Notes |
|---|---|---|
| Id | guid | Request identifier |
| StudentId | guid | Related student |
| Subject | varchar | Request subject |
| Details | text | Request details |
| SafetyValveText | text | Safety valve text |
| Urgency | enum | Priority |
| Status | enum | Request status |
| AssignedCounselorId | guid | Optional counselor |
| IdempotencyKey | varchar | Deduplication key |
| RowVersion | byte[] | Concurrency token |

### `RefreshTokens`

| Column | Type | Notes |
|---|---|---|
| Token | varchar | Stored refresh token |
| Subject | varchar | User subject |
| ExpiresAt | datetimeoffset | Expiry timestamp |

## 6) Relationship summary

- `enrollments.student_id` -> `user.id`
- `enrolled_subjects.enrollment_id` -> `enrollments.id`
- `class_offerings.subject_id` -> `subjects.id`
- `grades.enrolled_subject_id` -> `enrolled_subjects.id`
- `student_clearance.student_id` -> `user.id`
- `student_payments.student_id` -> `user.id`
- `library_reservations.student_id` -> `user.id`
- `scholarship_applications.student_id` -> `user.id`
- `student_assignments.student_id` -> `user.id`
- `document_requests.student_id` -> `user.id`
- `course_shifting_requests.student_id` -> `user.id`

## 7) Notes

- This department primarily uses MySQL queries via `StudentPortalDbService`.
- The portal also includes a guidance persistence context for counselor request and refresh-token tracking.
- The database design is operational and UI-driven rather than a fully normalized EF Core schema.

This file reflects the table structures used by the current Student Portal codebase and is intended as a quick database reference.
