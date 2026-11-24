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

## System Architecture: Model-View-Controller (MVC) Design (First Draft)

### 1. Architectural Overview & Separation of Concerns
The Gym Management System is implemented in ASP.NET Core MVC using a layered, interface-driven architecture that separates HTTP request orchestration, business rules, data persistence, and presentation:

- **`Models/`**: Domain entities mapped to SQL Server tables via Entity Framework Core (`Users`, `Staff`, `Members`, `MembershipPlans`, `Memberships`, `Payments`, `Attendance`).
- **`ViewModels/`**: Purpose-built data transfer objects scoped per module (`Account`, `Members`, `MembershipPlans`, `Memberships`, `Payments`, `Attendance`, `MemberPortal`, `AdminDashboard`) to prevent over-posting and decouple database schemas from UI contracts.
- **`Data/`**: `GymDbContext` (EF Core `DbContext` configuring primary/foreign keys and decimal precision) and `DbInitializer` (idempotent startup seeder for roles, accounts, plans, and demo records).
- **`Services/`**: Interface-backed business logic layer (`IMemberService`, `IStaffService`, `IMembershipService`, `IPaymentService`, `IAttendanceService`, `IMemberPortalService`, `IAdminDashboardService`) registered via Dependency Injection in `Program.cs`.
- **`Controllers/`**: Thin HTTP controllers enforcing `[Authorize]` role policies, `[ValidateAntiForgeryToken]`, and `ModelState` validation while delegating all data access and domain calculations to injected services.
- **`Views/`**: Strongly typed Razor (`.cshtml`) templates bound to ViewModels, sharing a role-aware navigation shell in `Views/Shared/_Layout.cshtml`.

### 2. Model & Data Access Layer
Domain entities in `Models/` use Data Annotations (`[Key]`, `[Required]`, `[DataType]`) and `IValidatableObject` (such as verifying `checkOut > checkIn` in `Models/Attendance.cs`) to enforce structural integrity. `GymDbContext` translates LINQ queries into parameterized SQL statements, while read-only queries use `.AsNoTracking()` to reduce change-tracker overhead.

### 3. Service & ViewModel Layer
All domain rules reside inside `Services/` rather than controllers or views:
- **Membership Lifecycle (`MembershipService`)**: Computes `EndDate = StartDate.AddDays(DurationDays)` and derives real-time statuses (`Active`, `Expiring` within 7 days, `Expired`, `Inactive`).
- **Atomic Transactions (`PaymentService`)**: Wraps `Memberships` creation and `Payments` recording inside an EF Core database transaction (`BeginTransactionAsync`) and supports non-destructive receipt voiding (`isVoided = true`).
- **Attendance Guardrails (`AttendanceService`)**: Blocks check-ins for expired/inactive memberships and enforces a single open check-in (`checkOut == null`) per member per calendar day.
- **Claims-Scoped Isolation (`MemberPortalService`)**: Resolves member records exclusively from authenticated user claims (`ClaimTypes.NameIdentifier` / `ClaimTypes.Email`) and rejects cross-member ID requests (`GetPortalForMemberIdAsync`) to prevent Insecure Direct Object Reference (IDOR) vulnerabilities.
- **KPI Aggregations (`AdminDashboardService`)**: Computes active member counts, 7-day expirations, non-voided monthly revenue, daily check-ins, and new member sign-ups.

### 4. Controller & Security Pipeline
Controllers in `Controllers/` remain thin and declarative:
- **Authentication & RBAC**: Cookie authentication populates `HttpContext.User` claims on sign-in. Class- and action-level `[Authorize(Roles = "...")]` attributes enforce the role matrix (`Admin`, `Staff`, `Member`), while `[AllowAnonymous]` exposes public authentication endpoints.
- **CSRF Protection**: All state-mutating `POST` actions enforce `[ValidateAntiForgeryToken]`.

### 5. End-to-End MVC Request Flow (Example: Member Portal)
1. **Request**: An authenticated user with the `Member` role requests `GET /MemberPortal`.
2. **Middleware & Authorization**: ASP.NET Core Cookie Authentication validates the session cookie and `[Authorize(Roles = "Member")]` verifies role membership.
3. **Controller (`Controllers/MemberPortalController.cs`)**: Extracts `ClaimTypes.NameIdentifier` and `ClaimTypes.Email` from `HttpContext.User` and invokes `_memberPortalService.GetPortalForUserAsync(...)`.
4. **Service & Database (`Services/MemberPortalService.cs`, `GymDbContext`)**: Queries `Members`, `Memberships`, `Payments`, and `Attendance` filtered strictly by the resolved `memberId`, computes `DaysRemaining` and `MembershipStatus`, and populates `MemberPortalViewModel`.
5. **View (`Views/MemberPortal/Index.cshtml`)**: Renders the membership status card, payment history, and attendance log using the strongly typed `MemberPortalViewModel`.
6. 
## Phase 2 Summary: Portals, Analytics, Renewals & SOLID Refactor (`v0.2.0`)

### 1. Core Modules Delivered
- **Member Self-Service Portal (`MemberPortalController`, `IMemberPortalService`)**: Provides authenticated `Member` users with a read-only view of their active subscription status, remaining days, payment receipts, and check-in history (`MemberPortalViewModel`).
- **Admin Analytics Dashboard (`AdminDashboardController`, `IAdminDashboardService`)**: Aggregates five real-time KPIs—Active Members, Subscriptions Expiring in 7 Days, Monthly Non-Voided Revenue, Check-Ins Today, and New Members This Month—alongside 7-day check-in and 6-month revenue series for Chart.js (`AdminDashboardViewModel`).
- **Public Landing Page (`HomeController`, `ILandingPageService`)**: Exposes an `[AllowAnonymous]` storefront displaying active `MembershipPlans` sorted by price and duration alongside facility hours and contact details (`LandingPageViewModel`) without adding new database tables.
- **Expiry & One-Click Renewal Engine (`RenewalsController`, `IRenewalService`, `MembershipExpiryBackgroundService`)**: Surfaces a 7-day expiring and recently expired queue for `Admin` and `Staff`, executes atomic membership renewals, and runs a 24-hour background worker (`BackgroundService`) to deactivate past-due subscriptions.
- **Role-Based Post-Login Routing (`AccountController`)**: Routes authenticated users directly to their primary workspace upon sign-in (`Admin` $\rightarrow$ `/AdminDashboard`, `Staff` $\rightarrow$ `/Attendance`, `Member` $\rightarrow$ `/MemberPortal`).

### 2. Business Rules, Transactions & Security Controls
- **IDOR Prevention**: `MemberPortalService` resolves profiles strictly from authenticated session claims (`ClaimTypes.NameIdentifier` and `ClaimTypes.Email`) and explicitly rejects cross-member ID lookups (`GetPortalForMemberIdAsync`).
- **Atomic Renewal Transactions**: `RenewalService.RenewMembershipAsync` wraps old membership deactivation (`isActive = false`), new `Memberships` creation, and `Payments` insertion inside an EF Core database transaction (`BeginTransactionAsync`) with full rollback on failure.
- **Renewal Date Continuity**: Unexpired renewals start on `CurrentEndDate + 1 day` to preserve remaining paid days, whereas expired or unassigned renewals start on the transaction date (`Today`). Non-cash renewals (`GCash`, `Maya`, `Bank Transfer`, `Card`) enforce a non-empty `ReferenceNumber`.
- **Edge-Case Resilience**: Safely handles unassigned members (`"No Membership"` state), blocks deactivated members (`isActive = false`) from checking in, prevents duplicate same-day open check-ins, and guards empty-table revenue aggregations against null sums.

### 3. SOLID Principles Applied
- **Single Responsibility (SRP)**: Controllers handle HTTP routing, `[Authorize]` policies, `[ValidateAntiForgeryToken]`, and `ModelState` validation, delegating all queries and domain rules to `Services/`.
- **Open/Closed (OCP)**: Plan durations, pricing tiers, and scheduled expiry sweeps (`MembershipExpiryBackgroundService`) extend system behavior without modifying existing controller actions.
- **Liskov Substitution (LSP)**: All `I*Service` implementations honor identical interface contracts and nullability guarantees across SQL Server and EF Core's `InMemory` test provider.
- **Interface Segregation (ISP)**: Capabilities are split into role-focused interfaces (`IMemberPortalService`, `IAdminDashboardService`, `ILandingPageService`, `IRenewalService`) rather than a monolithic service interface.
- **Dependency Inversion (DIP)**: All domain controllers depend exclusively on `I*Service` abstractions registered via Dependency Injection in `Program.cs` (including decoupling `GymDbContext` from `RenewalsController` via `GetStaffIdByUserIdAsync`).

### 4. Automated Verification (`dotnet test` — 53/53 Passing)

| Test Suite | Scope & Verified Behaviors |
| :--- | :--- |
| `MemberPortalServiceTests` | Claims-scoped data isolation, remaining-day calculations, cross-member IDOR blocking, `[Authorize(Roles = "Member")]` |
| `AdminDashboardServiceTests` | All 5 KPI calculations, exclusion of voided payments (`isVoided = true`), 7-day & 6-month chart aggregations, Admin-only RBAC |
| `LandingPageServiceTests` | Active-only plan filtering, ascending price sort, empty-table safety, `[AllowAnonymous]` on `HomeController.Index` |
| `RenewalServiceTests` & `BusinessRulesTests` | `EndDate + 1` vs. `Today` renewal start rules, atomic transaction rollback, non-cash reference validation, duplicate check-in prevention, daily expiry sweep |
| `Tier1EdgeCaseTests` | Unassigned member safe defaults, deactivated member check-in block, zero-KPI aggregation on empty database |
| `SolidArchitectureTests` & `AccessControlTests` | Reflection audit confirming `I*Service` implementation on all domain services, zero direct `GymDbContext` injection in domain controllers, and exact `[Authorize]` role policies across all controllers |

# Phase 3 Session Notes — Flexify

**Session Focus:** AI Front-Desk Chatbot Stabilization & Live Gemini Integration, Responsive UI/UX Refinements, and Expanded Idempotent Demo Database Seeding  
**Target Branch / Milestone:** Phase 3 Finalization & Demonstration Readiness

---

## 1. Summary of Phase 3 Deliverables

During this session, four targeted improvements were implemented and verified across the presentation, service, and data initialization layers:

1. **Chatbot Widget Asset Resolution & Global Initialization:** Fixed broken relative paths (`../wwwroot/assets/...` $\rightarrow$ `~/assets/...`) in `_ChatbotWidget.cshtml` and removed an early `return;` guard in `wwwroot/js/site.js` so the assistant initializes on every layout page, not just `/ReceiptOcr/Upload`.
2. **Live Gemini Integration & Resilient Role-Aware Fallback:** Configured Google's OpenAI-compatible Gemini endpoint (`gemini-3.8-flash`) via `dotnet user-secrets`, added retry/fallback resilience for transient `503`/`429` capacity spikes in `LlmChatService`, and upgraded `ChatbotService` to handle **Admin/Staff** operational queries alongside **Member** self-service queries and offline fallbacks.
3. **Responsive UI/UX Enhancements:** Added tablet (`<= 960px`) sidebar collapse rules, mobile (`<= 640px`) layout adjustments, sticky translucent public topbars, horizontal-scrolling prompt chips, and subtle hover/active micro-interactions in `wwwroot/css/site.css`.
4. **Expanded Idempotent Demo Seeding:** Refactored `Data/DbInitializer.cs` from `AnyAsync()` table-level checks to key-based `Ensure*Async` helpers, populating 6 membership plans, 10 members across all lifecycle states, 11 payment records across 5 payment methods (including 1 voided transaction), and a 7-day attendance history with 2 active gym-floor check-ins.

---

## 2. Detailed Changes by Component

### A. Frontend Chatbot Widget & Global JavaScript (`_ChatbotWidget.cshtml`, `wwwroot/js/site.js`)
* **Root Cause 1 (Missing Launcher Icon):** `_ChatbotWidget.cshtml` referenced `../wwwroot/assets/gym-robot-logo-chat-bubble-flexify-v2.png`. Because ASP.NET Core serves `wwwroot` as the web root (`/`), relative traversal above `wwwroot` failed to resolve in the browser.
  * **Fix:** Retained the asset inside `wwwroot/assets/` and updated both `<img>` tags to `~/assets/gym-robot-logo-chat-bubble-flexify-v2.png`.
* **Root Cause 2 (Unresponsive Launcher Button):** `wwwroot/js/site.js` executed `if (!form) { return; }` at the top of `DOMContentLoaded` when `#receiptUploadForm` was absent, terminating script execution before `#fxChatbot` event listeners were attached.
  * **Fix:** Scoped receipt OCR setup inside `if (form) { ... }` so `#fxChatbot` initializes independently across all views.

### B. LLM Integration & Role-Aware Chatbot Service (`LlmChatService.cs`, `ChatbotService.cs`, `ChatbotController.cs`)
* **Secure Configuration (`dotnet user-secrets`):**
  * Stored `Llm:ApiKey` outside source control using User Secrets.
  * Set `Llm:BaseUrl` to `https://generativelanguage.googleapis.com/v1beta/openai/` and updated `Llm:Model` to `gemini-3.8-flash` after resolving deprecated `gemini-2.5-flash` 404 responses.
  * Hardened `LlmChatService.cs` with `string.IsNullOrWhiteSpace` checks so empty strings in `appsettings.json` do not override default URLs/models, and added automatic retry logic for transient HTTP `429` / `503` responses.
* **Role-Aware Context & Smart Fallback (`ChatbotService.cs`):**
  * **Member Role (`User.IsInRole("Member")`):** Injects verified subscription plan, status, expiration date, days remaining, latest non-voided payment, and recent check-in logs into the system prompt and local fallback matcher.
  * **Admin / Staff Roles (`User.IsInRole("Admin") || User.IsInRole("Staff")`):** Queries `GymDbContext` via `LoadAdminSnapshotAsync` to supply live active/inactive member counts, active subscriptions, memberships expiring within 7 days, monthly non-voided revenue, and today's gym-floor check-in metrics.
  * **Dynamic Suggested Prompts:** Added `GetDefaultSuggestedPrompts(ClaimsPrincipal user)` on `IChatbotService` so `GET /Chatbot/Prompts` tailors prompt chips to Public, Member, or Staff/Admin sessions.
  * **Unit Test Compatibility:** Kept `GymDbContext? context = null` as an optional constructor parameter and preserved `Intent = "Fallback"` so existing `ChatbotServiceTests` pass without breaking.

### C. Responsive UI/UX Refinements (`wwwroot/css/site.css`)
* **Tablet Breakpoint (`@media (max-width: 960px)`):** Converts `.fx-admin` from a fixed side-by-side layout into a stacked column layout with a wrapping top navigation bar, preventing cramped content areas on mid-sized viewports.
* **Mobile Breakpoint (`@media (max-width: 640px)`):** Stacks page header buttons and form actions to full width, collapses `.fx-choice-grid` to a single column, compacts table padding, and constrains `.fx-chat__window` to `calc(100vw - 24px)` and `72vh` max-height.
* **Prompt Chips & Tactile Feedback:** Converted `.fx-chat__prompts` to a single horizontal scrollable strip (`flex-wrap: nowrap; overflow-x: auto`) to preserve vertical chat transcript space, made `.fx-topbar` sticky with `backdrop-filter: blur(8px)`, and added subtle hover elevation on `.fx-stat` and `.fx-gym-card`.

### D. Expanded Idempotent Demo Database Seeder (`Data/DbInitializer.cs`)
* Replaced table-level `if (!await _context.<Table>.AnyAsync())` guards with idempotent lookup helpers (`EnsureStaffAsync`, `EnsurePlanAsync`, `EnsureMemberAsync`, `EnsureMembershipAsync`, `EnsurePaymentAsync`), allowing new demo records to seed safely onto existing databases without running new EF Core migrations.

---

## 3. Seeded Demo Accounts & Test Scenarios

| Role | Email | Password | Seeded State / Demo Scenario |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@gmail.com` | `Admin@12345` | Full access to Admin Dashboard, Staff management, Plans, and permanent deletes |
| **Staff** | `staff@gmail.com` | `Staff@12345` | Front-desk operations: Check-in/out, Payments, Receipt OCR |
| **Member (Active)** | `carlos.mendoza@gmail.com` | `Member@12345` | Active **Quarterly Pro** plan (71 days remaining), GCash payment history, multi-day check-in logs |
| **Member (Expiring)** | `bianca.lim@gmail.com` | `Member@12345` | **Monthly Basic** expiring in **3 days** (triggers 7-day renewal alert) |
| **Member (Expired)** | `rafael.torres@gmail.com` | `Member@12345` | **Monthly Basic** expired **10 days ago** (demonstrates check-in block & renewal flow) |
| **Member (Unassigned)** | `sofia.garcia@gmail.com` | `Member@12345` | Registered member with no subscription assigned yet |
| **Member (Inactive)** | `denise.navarro@gmail.com` | `Member@12345` | Deactivated/locked-out account (`isActive = false`) |

---

## 4. Verification Checklist

* [x] Chatbot launcher renders `~/assets/gym-robot-logo-chat-bubble-flexify-v2.png` and opens/closes cleanly across public, member, and admin pages.
* [x] `dotnet user-secrets` configured for `Llm:ApiKey`, `Llm:BaseUrl`, and `Llm:Model` (`gemini-3.8-flash`).
* [x] Role-aware chatbot responses verified for **Member** (`"When does my membership expire?"`) and **Admin/Staff** (`"How many active members do we have?"`), both online and under simulated fallback.
* [x] Expanded demo dataset verified on `/AdminDashboard` (6 active members, 2 expiring within 7 days, ₱9,250 monthly revenue, 7-day attendance bar chart) and `/MemberPortal`.
* [x] Unit test suite (`dotnet test --no-incremental`) passing.