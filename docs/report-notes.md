## Sprint 1 Git Evidence (v0.1.0)

### 1. Branching Strategy & Pull Request Traceability
The repository follows a structured Git workflow using `main` for tagged releases (`v0.1.0`), `develop` for integration, `feature/*` branches for isolated module development, and `fix/*` branches for defect resolution and security hardening. All changes were integrated into `develop` via Pull Requests rather than direct commits to `main`.

| Branch Name | Module / Scope | Key Commits / Artifacts | Integration Status |
| :--- | :--- | :--- | :--- |
| `feature/project-setup` | ASP.NET Core MVC setup, EF Core packages, SQL Server config | `349b720`, `8fd0f62` | Merged into `develop` |
| `feature/data-model` | Entities, `GymDbContext`, initial migrations, `DbInitializer` | `ac3378c`, `2894884` | Merged into `develop` |
| `feature/auth` | Cookie auth, `AccountController` (Register, Login, Logout), `[Authorize]` | `06e2117` | Merged into `develop` |
| `feature/members-crud` | `IMemberService`, `MemberService`, Members CRUD & deactivation | `30d072b` | Merged into `develop` |
| `feature/memplans-crud` | Membership plans CRUD (name, duration in days, price) | `38bb444` | Merged into `develop` |
| `feature/staff-crud` | Admin-only Staff CRUD (`IStaffService`, `StaffService`) | `688f71f` | Merged into `develop` |
| `feature/membership-crud` | Plan assignment (`EndDate` calculation), status logic, xUnit setup | `4371d87` | Merged into `develop` |
| `feature/payments` | Transactional membership + payment save, date/member filter, voiding | `PaymentService`, `PaymentServiceTests` | Merged into `develop` |
| `feature/attendances` | Member search check-in, check-out, expired block, 1 open check-in/day | `AttendanceService`, `AttendanceServiceTests` | Merged into `develop` |
| `fix/access-control` | Role-based `[Authorize]` matrix audit, reflection tests, enriched seed data | `AccessControlTests`, `DbInitializer` | Merged into `develop` |
| `main` (`v0.1.0`) | Sprint 1 release milestone | Merged from `develop` and tagged `v0.1.0` | Released |

### 2. Local & Remote Branch List
```text
* 227cf96 (HEAD -> develop, origin/fix/access-control, fix/access-control) (fix): enrich the db with seed data and test access controls
* 73d4693 (origin/feature/attendances, feature/attendances) (add): implemented the services and crud operations for attendance
* 196f6da (origin/feature/payments, feature/payments) (add)payments: Implement Payments CRUD, transactional membership + payment save, void confirmation, and xUnit tests
* 4371d87 (origin/feature/membership-crud, feature/membership-crud) (add)memb-crud: refactored the crud operation on membership model
* 688f71f (origin/feature/staff-crud, feature/staff-crud) (add)staff-crud: implement admin-only staff crud with IStaffService and StaffServices
* 38bb444 (origin/feature/memplans-crud, feature/memplans-crud) (add): membership plans primary crud operations
* 30d072b (origin/feature/members-crud, feature/members-crud) (add): implement members crud operations and views
* 06e2117 (origin/feature/auth, feature/auth) (add)feature: Complete AccountController, auth views, default authorize policy, and mark some tasks done
* 2894884 (origin/feature/data-model, feature/data-model) (chore): update GymDbContext and mark seeding task complete
* ac3378c (add): code-first approach, dbInitializer seed class, and db migrations
* 8fd0f62 (origin/feature/project-setup, origin/develop) (add): configured packages and database migrations to ms sql server
* 349b720 (add): config c# packages and confirm sql server connection
| * e600cf2 (origin/main, main) checkpoint: consolidate tasks in day 1
|/  
* 47f7b82 add(chore): Initialize project repository
```