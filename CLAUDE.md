# Learna

A school management platform inspired by Lectio, targeting the Thai market while staying
reusable across school systems. The product vision, roles, and domain model are defined in
[dev-scope.md](dev-scope.md) at the repo root — read it before making product decisions.

## Stack & running locally

- **Backend**: ASP.NET Core 8 Web API — `src/backend/Learna.Api` (host), `Learna.Core`
  (entities/interfaces), `Learna.Infrastructure` (EF Core + MySQL via Pomelo, repositories),
  `Learna.Tests`.
- **Frontend**: Angular (standalone components, **zoneless**) + Angular Material —
  `src/frontend/learna-app`.
- **Database**: MySQL 8 via `docker-compose.yml` (`docker compose up -d`), container
  `learna-mysql`, root/root, db `learna_dev`.

Run:
```bash
docker compose up -d                              # MySQL
cd src/backend/Learna.Api && dotnet run           # API on http://localhost:5157 (Swagger at /swagger)
cd src/frontend/learna-app && ng serve            # Angular on http://localhost:4200
```
Admin login: `admin@learna.com` / `Admin123!` (seeded in Development).

Backend tests: `dotnet test` from `src/backend` or the solution root.
Frontend build: `ng build` from `src/frontend/learna-app`.

Migrations are created/applied from `Learna.Infrastructure` pointing at `Learna.Api` as the
startup project:
```bash
cd src/backend/Learna.Infrastructure
dotnet ef migrations add <Name> --project . --startup-project ../Learna.Api
dotnet ef database update --startup-project ../Learna.Api --project .
```

## Hard conventions

- **Repository-per-entity + DTOs + Admin-only writes.** Controllers depend on
  `I<Entity>Repository` interfaces from `Learna.Core.Interfaces`, implemented in
  `Learna.Infrastructure.Repositories`. API DTOs live in `Learna.Api/DTOs`; entities never
  cross the wire directly. Write endpoints for reference/academic data are Admin-only.
- **Zoneless Angular — all async component state must be signals.** Never read `.value` off
  an RxJS subject or other mutable state inside `computed()`; it won't be tracked reactively
  and will only reflect the value at first read. Two production bugs already came from this.
  Use `signal()`/`computed()`/`toObservable()` throughout.
- **All text fields are Unicode/Thai-capable.** Never assume ASCII-only names, addresses, or
  free-text fields.
- **Class/term/year names are free text.** Never assume a `"1A"`-style format — Thai schools
  use forms like `ม.1/1`, `ป.6`, etc.
- **UI text goes through translation keys (Transloco), never hardcoded strings** — see the
  i18n setup added in Work Order 4. New UI text must be added to both `en.json` and `th.json`.

## Standing decisions

- `PersonName` (owned type shared by Student/Teacher/Guardian) has **no `DisplayName`** field
  — this was a deliberate owner decision. Do not add one; compose display strings from
  title/firstName/lastName/nickname where needed.
- **Class ≠ SubjectGroup.** `SchoolClass` is the administrative/homeroom group;
  `SubjectGroup` is the teaching group for a specific subject/term. Never collapse them —
  a class's members and a subject group's enrollments are tracked separately
  (`ClassMembership` vs `Enrollment`), both history-preserving (join/left dates).
- **No Subject seed data without an owner-provided Thai subject list.** Don't invent Thai
  subject names.
- **Messaging (WO16).** User-to-user 1:1 and group messaging exists (`Conversation`,
  `ConversationParticipant`, `Message`). Messages are **immutable** — no edit/delete
  endpoints. Reach is gated by a school-configurable role-pair permission matrix
  (`MessagingPolicyRule`, sibling endpoints under `api/school-settings/messaging-policy`;
  default all pairs allowed). **Admins can read (not send in) any conversation** —
  the frontend must always show the transparency notice about this in thread views. See
  the "Messaging" section in `dev-scope.md` for the full model.

## Verification practice

"It compiles" is not done. Before reporting a change complete, drive the actual running app
(API via Swagger/HTTP, frontend via browser) through the relevant flow. Playwright is a
devDependency of `src/frontend/learna-app` for this purpose (`npm install` from that folder
pulls it in). For a quick one-off check, a throwaway script driven by `node` against the
installed `playwright` package works fine — launch Chromium, navigate to
`http://localhost:4200`, log in, and drive the flow under test; no Playwright test runner or
config is set up in this repo, so there's no `npx playwright test` suite to run. Make sure
`ng serve` and the API are both running first.
