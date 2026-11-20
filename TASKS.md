**Wed Nov 18**

- ✅ Decide the .NET and Visual Studio version with your team and instructor; add a `global.json`
- ✅ Create the public GitHub repo with a README and a Visual Studio `.gitignore`; create `main` and `develop`; agree the branch and commit rules
- ✅ On `feature/project-setup`: create the ASP.NET Core MVC project, add the EF Core SQL Server packages, confirm the SQL Server connection, open a pull request into `develop`
- [ ] Write 15 to 20 user stories (as Admin, Staff, Member and Visitor) in `docs/requirements.md`
- [ ] Copy the instructor's report template into `docs/report/` and start `docs/report-notes.md`

**Thu Nov 19**

- ✅ On `feature/data-model`: write the entities, `GymDbContext`, relationships and the first migration; run `Update-Database`
- [ ] Draw the ERD and save it in `docs/`
- ✅ Seed the admin account and demo data with a seed class; merge the pull request
- [ ] Build the layout shell: navbar that changes by role, footer, shared styles
- [ ] Sketch wireframes: landing page, register and login, member portal, admin dashboard, check-in screen

**Fri Nov 20: authentication, the graded challenge**

- ✅ On `feature/auth`: an `AccountController` with Register, Login and Logout views and view models, using your `Users` table and cookie authentication
- ✅ Registration saves a Users row with a hashed password and the role Member, creates the Members row, then signs the user in; keep `GmailOnlyAttribute` on the registration email only, so seeded accounts are not blocked
- ✅ Login checks the password hash, signs in with the user ID and role as claims, and redirects by role; the seeded admin can log in
- ✅ `[Authorize]` as the default, an Access Denied page, optional lockout after repeated failures
- ✅ Write and run the auth test cases by hand (valid and invalid registration, wrong password, a direct URL without logging in); merge the pull request

Authentication comes first on purpose: your instructor called it the challenge, and every other screen depends on it.

## Phase 1: Core CRUD, sprint 1 (Nov 21 to 27)

SDLC stage: implementation and testing, sprint 1. Goal: every core module has full CRUD, and staff can register a member, sell a plan, record the payment and check the member in. Done when that path works end to end on the seed data, and sprint 1 is merged to `main` and tagged `v0.1.0`.

Every module follows the same pattern: a feature branch, an entity, an interface and service registered in `Program.cs`, a thin controller, view models, views for Index, Details, Create, Edit and Delete, three or four test cases, and a pull request.

**Sat Nov 21: members**

- ✅ `feature/members-crud`: `IMemberService` and `MemberService`; list with search and paging, details, create, edit, deactivate with a confirmation page
- ✅ Write the Members test cases and add three lines to the report notes

**Sun Nov 22: plans and staff**

- ✅ `feature/plans-crud`: membership plans CRUD (name, duration in days, price)
- ✅ `feature/staff-crud`: staff CRUD, admin only

**Mon Nov 23: memberships**

- ✅ Assign a plan to a member: `EndDate` = `StartDate` + `DurationDays`
- ✅ Computed Status and Days Remaining in the service, shown on member details
- ✅ Create the xUnit test project; test days remaining and status, including the expired and expiring cases

**Tue Nov 24: payments**

- ✅ Record, list (filter by date and member), edit and void with a confirmation page; membership and payment save in one transaction
- ✅ Test cases for amounts and voiding

**Wed Nov 25: attendance**

- ✅ Staff check-in by member search, check-out button, list, admin delete
- ✅ Warn or block when the membership is expired; one open check-in per member per day; test cases for both

**Thu Nov 26: access control pass**

- ✅ Apply role attributes across every controller; test the role-by-feature matrix and fix gaps on `fix/*` branches
- ✅ Enrich the seed data

**Fri Nov 27: sprint 1 review**

- ✅ Run every sprint 1 test case and record actual results; log bugs and fix them on `fix/*` branches
- ✅ Merge `develop` into `main` and tag `v0.1.0`
- ✅ Report: MVC trace for the Members module, and Git evidence (branch list, network graph, pull requests)

## Phase 2: Portal, dashboard and tests, sprint 2 (Nov 28 to Dec 4)

SDLC stage: implementation and testing, sprint 2. Goal: members see their own status, the admin sees the business at a glance, the gym has a public page, and the code is refactored and tested. Done when a member login shows status, days remaining and payment history, the landing page lists the plans, and sprint 2 is tagged `v0.2.0`.

**Sat Nov 28: member portal**

- ✅ Home card with status, days remaining and expiry date; payment history and attendance history for the signed-in member only
- ✅ Cross-member access test (member A opens member B's URL and fails)

**Sun Nov 29: admin dashboard**

- ✅ Number cards: active members, expiring in 7 days, revenue this month, check-ins today, new members
- ✅ Chart.js charts for check-ins and revenue, only if time allows

**Mon Nov 30: tier 1 complete**

- ✅ Bug bash on tier 1 as each role; fix on `fix/*` branches
- ✅ Report: first draft of the MVC section from your notes

**Tue Dec 1: public landing page**

- [ ] Landing page: hero, plans and prices read from MembershipPlans, facility info, contact details, links to Register and Login (no new table needed)
- [ ] Check it at phone width

**Wed Dec 2: expiry and renewal**

- [ ] Status shown everywhere as Active, Expiring or Expired; an expiring-soon list for staff
- [ ] One-click renew using the renewal rule, which also creates the payment; xUnit tests for the renewal date
- [ ] Optional: a daily background service that flags expired memberships

**Thu Dec 3: SOLID refactor and tests**

- [ ] Extract an interface for each service, check that controllers are thin, and confirm everything is registered in `Program.cs`
- [ ] xUnit tests for payment and attendance rules; write any missing manual test cases

**Fri Dec 4: sprint 2 review**

- [ ] Run all sprint 2 test cases; merge `develop` into `main`; tag `v0.2.0`
- [ ] Report: SOLID write-up with a code snippet for each principle

## Phase 3: Extras and report draft (Dec 5 to 9)

SDLC stage: final implementation, then feature freeze at the end of Wed Dec 9. Goal: add optional extras in time-boxed slices and finish draft v1 of every report chapter. If tier 2 is not finished by Dec 4, use these days to finish it and skip the extras. Extras menu: the chatbot, receipt OCR, CSV export and email, or the tables you dropped (promotions, inquiries, audit log). Pick at most two, because a finished report beats a third feature. The days below start with the chatbot; swap any slot for another extra.

**Sat Dec 5 and Sun Dec 6: chatbot**

- [ ] Intent router, account answers from the database using the session's member ID, chat widget on the portal and landing page
- [ ] FAQ list of 10 to 15 answers kept in code or a JSON file, plus a keyword matcher; add the LLM call (with a timeout) only if the keyword version already works
- [ ] Report: QA chapter with the test case table, and the SDLC chapter complete

**Mon Dec 7: receipt OCR**

- [ ] Upload, validate, OCR, pre-filled confirmation form, save
- [ ] Test on 10 real receipts and record how many fields were right

**Tue Dec 8: exports and email**

- [ ] CSV export for members, payments and attendance behind an `IReportExporter`
- [ ] `IEmailService` with MailKit and a "send reminder" button on the expiring-soon list

**Wed Dec 9: freeze**

- [ ] Merge `develop` into `main` and tag `v0.9.0`
- [ ] Report draft v1 complete: every chapter written, screenshots listed

The design notes below apply only to the extras you choose to build.

### Chatbot design

The split that keeps it safe and reliable: the database answers questions about the person, the LLM only answers general questions.

1. Classify each message with simple keyword rules first. Words like "days left", "expire", "status", "my payments" mean an account question; everything else is an FAQ question.
2. Account question: read the member ID from the session claims, query the database, and fill a fixed template ("Your Gold plan is Active, 12 days remain, ends Dec 30"). The LLM never sees this data and never decides who the member is.
3. If the user is not logged in as a member, answer "please log in to check your membership".
4. FAQ question: keep 10 to 15 questions and answers (hours, rules, location) in a C# list or a JSON file in the project, with no extra table. Plan names and prices come from MembershipPlans so they never go stale. Send the question plus that text to the LLM with a short instruction to answer only from it and say "I don't know" otherwise. Set a timeout of about 5 seconds.
5. If the call fails, times out or no key is set, match the question's words against each entry's keywords in that same list. If nothing matches, show the gym's contact details.
6. Rate-limit chat requests per user, and keep API keys on the server only.

### Receipt OCR design

The rule: OCR suggests, staff decide. Nothing is saved from OCR without a confirmation click.

1. Staff uploads a photo. The server checks file type (jpg, png, pdf) and size (5 MB or less) and saves it under a random name.
2. The OCR service (behind `IOcrService`) returns the raw text. Simple rules pull out candidates: the amount (the number nearest the word "total"), the date, and a reference number.
3. The confirmation page shows the image beside a form pre-filled with those candidates. Staff fix anything wrong and pick the member and membership.
4. On Confirm, save the payment with the receipt path. This needs one small migration: add nullable `ReceiptImagePath` and `OcrRawText` columns to Payments.
5. If OCR finds nothing, the form opens blank and the flow still works.
6. Skip waiver scanning unless you have time left: it would need its own table or a path column on Members.

### Exports and reminders

Write CSV by hand from a query (no library). Add Excel with ClosedXML only if the other items are done. For email, use a test inbox service or a throwaway account while developing, and log every send.

## Phase 4: QA, report and submission (Dec 10 to 15)

SDLC stage: testing, release and maintenance. Goal: no new features. Prove the app works, finish the report, and submit early. Done when the PDF and the GitHub link are submitted.

**Thu Dec 10: QA execution day**

- [ ] Run every manual test case and record the actual result and pass or fail; run the full xUnit suite
- [ ] Log bugs; fix serious ones on `fix/*` branches with pull requests
- [ ] Security pass: another member's URL, a renamed .exe uploaded as an image, anti-forgery tokens on every POST form, no secrets in git

**Fri Dec 11: polish and evidence**

- [ ] Empty states, clear validation messages, a friendly error page; README with setup steps, connection string and seeded logins
- [ ] Capture every screenshot the report needs: each role's screens, ERD, branch graph, pull requests, tags, test results

**Sat Dec 12: write the final report**

- [ ] Complete every chapter in the instructor's template, with code snippets and file paths: CRUD matrix, MVC trace, SOLID table, Git evidence, QA results

**Sun Dec 13: proofread and export**

- [ ] Proofread against the template and the rubric; have a teammate read it; export the PDF
- [ ] Rehearse the demo twice, resetting the database from the seed each time

**Mon Dec 14: release and submit**

- [ ] Merge `develop` into `main` and tag `v1.0.0`
- [ ] Open the repo link in a private browser window to confirm it is public; clone fresh and run it from the README alone
- [ ] Submit the PDF and the GitHub link through the submission link

**Tue Dec 15: deadline, 11:59 PM**

- [ ] Buffer only. No code changes unless something is completely broken

## Risks and cut list

The graded items come first, so the biggest risks are about authentication, the Git history and the report.

| Risk | Early warning | Fallback |
| --- | --- | --- |
| Authentication takes longer than a day | Nov 20 ends without working Register and Login | Build Register first (hashed password), then Login with the cookie; cut lockout and extra rules if needed; ask your instructor early |
| Git history looks like one big commit | Only `main` has commits by Nov 22 | Daily pull requests into `develop`; no direct commits; a teammate reviews |
| Report left to the last weekend | `docs/report-notes.md` has no entries by Nov 27 | Reserve 60 minutes a day; hold the draft v1 deadline of Dec 9 |
| Visual Studio or .NET version mismatch | A teammate cannot build a fresh clone | Decide on Nov 18, add `global.json`, check every machine builds on Nov 19 |
| The template differs from this plan | You paste the template and its headings do not match | Re-map the report track to the template's headings; the evidence stays the same |
| Phase 1 runs late | The Nov 27 staff path does not work end to end | Seed staff accounts instead of building staff CRUD; keep attendance to check-in and list |
| An outside service fails (OCR, LLM, email) | The first test call fails | Drop that extra; none of them is in the course criteria |
| Migration conflicts | Two people change the model on the same day | One person owns migrations; merge them through `develop` only |

If you fall behind, cut in this order and stop as soon as you are back on schedule:

1. Excel and PDF export (keep CSV)
2. Email and reminders
3. Receipt OCR
4. LLM answers
5. The chatbot
6. Dashboard charts
7. Promotions, inquiries and audit log, if you added them

Never cut: Register and Login, full CRUD on the core entities, the branch and pull request history, the test results, and the report.

## Demo script and submission checklist

A seven-minute demo that walks through the rubric as well as the features:

1. Visitor: open the landing page, register a new account and log in.
2. Member: show status, days remaining and payment history; try another member's URL and get blocked.
3. Staff: create, read, update and delete a member.
4. Staff: assign a plan, record a payment and check the member in; then block an expired member and renew them with one click.
5. Admin: dashboard, staff and plans CRUD, the payments list.
6. Any extras you built: the chatbot answering "how many days are left?", a receipt scan, a CSV export.
7. GitHub: branches, merged pull requests and tags, then the report's test table and an xUnit run.

Before you submit:

- [ ] The GitHub repo is public and opens in a private browser window
- [ ] `main` holds the `v1.0.0` release, and the `develop`, feature and fix branches and pull requests are visible
- [ ] Every team member's commits appear in the history
- [ ] Migrations are committed and `Update-Database` builds the database from scratch
- [ ] The README lists setup steps, the connection string format and the seeded logins
- [ ] No keys or passwords in the repo
- [ ] The report PDF follows the instructor's template and covers SDLC, MVC, SOLID, Git and QA testing, each tied to file names or screenshots
- [ ] The test case table has actual results filled in
- [ ] The GitHub link and the PDF are submitted through the submission link before Dec 15, 11:59 PM
