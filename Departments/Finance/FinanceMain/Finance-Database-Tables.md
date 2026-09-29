# Finance Department Database Tables

This reference documents the Finance department's MySQL tables created or used by `FinanceDbService`. Finance also has a SQL Server connection configured, but its current guidance services use in-memory stores rather than SQL Server persistence.

## Database connections

- MySQL `DefaultConnection`: host `100.98.41.69`, port `3306`, database `mydb`.
- SQL Server `CampusSystemDb`: `localhost,1433`, database `CampusSystemDb` (registered as a generic DbContext factory; no Finance-owned SQL Server entities are mapped here).

## MySQL tables

### `student_payments`

| Column | MySQL type | Constraints / notes |
|---|---|---|
| id | INT | Auto-increment primary key |
| student_id | INT | Student user ID |
| receipt_no | VARCHAR(60) | Unique receipt number |
| amount | DECIMAL(10,2) | Payment amount |
| pay_date | DATETIME | Payment date/time |
| payment_method | VARCHAR(50) | Payment method |
| status | VARCHAR(30) | Default `Paid` |
| description | VARCHAR(255) | Payment description |

### `student_clearance`

| Column | MySQL type | Constraints / notes |
|---|---|---|
| id | INT | Auto-increment primary key |
| student_id | INT | Student user ID |
| department_name | VARCHAR(100) | Department being cleared |
| school_year | VARCHAR(50) | Academic year |
| semester | VARCHAR(50) | Academic term |
| status | VARCHAR(50) | Default `Cleared` |
| cleared_by | VARCHAR(150) | Default `Dean / Head` |
| cleared_at | DATETIME | Nullable clearance timestamp |
| remarks | TEXT | Nullable notes |
| uniq_stud_dept | unique key | `(student_id, department_name, school_year, semester)` |

### `fee_assessments`

| Column | MySQL type | Constraints / notes |
|---|---|---|
| id | INT | Auto-increment primary key |
| assessment_number | VARCHAR(60) | Unique assessment number |
| student_id | INT | Student user ID |
| student_number | VARCHAR(50) | Student number |
| student_name | VARCHAR(150) | Student name |
| program | VARCHAR(100) | Academic program |
| school_year | VARCHAR(50) | Academic year |
| semester | VARCHAR(50) | Academic term |
| total_amount | DECIMAL(10,2) | Assessed amount |
| total_paid | DECIMAL(10,2) | Defaults to `0.00` |
| status | VARCHAR(30) | Defaults to `Unpaid` |
| created_at | DATETIME | Defaults to current timestamp |

## Other shared MySQL table used

### `user`

Finance queries `user.id` by `student_id_number` to resolve a student. Its schema is shared with other department apps and is not created by Finance.

## Relationships and sharing

- `student_payments.student_id` and `student_clearance.student_id` refer to the shared MySQL student/user identifiers.
- `student_payments` and `student_clearance` are shared with Student Portal and other campus department workflows.
- The Finance application creates/verifies the three Finance-related MySQL tables during startup in resilient background initialization.

## Notes

- MySQL table DDL above is from `Services/FinanceDbService.cs`.
- Finance request/guidance stores are currently in memory; they do not create persistent Finance SQL tables.
