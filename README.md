# RaceDay - Portfolio of Evidence (Part 1 and Part 2)

RaceDay is a system for organising and entering running, walking and cycling events.
This repository contains the planning documents (Part 1) and the RESTful ASP.NET Core Web API (Part 2).

```
/docs                      ERD (PNG + Mermaid), API endpoint plan
/sql/RaceDay.sql           SQL Server script (schema + sample data)
/src/RaceDay.Api           ASP.NET Core 8 Web API (EF Core code-first, SQL Server)
/tests/RaceDay.Api.Tests   xUnit tests (run against the real API pipeline, in-memory DB)
/.github/workflows         GitHub Actions CI (build + test on every push)
```

## Part 1 - Planning

| Section | File |
|---|---|
| A - ERD (6 entities: Roles, Users, Events, Categories, Enrolments, Results) | `docs/RaceDay_ERD.png` (text version `docs/ERD.md`) |
| B - API endpoint plan (22 endpoints) | `docs/API_Endpoint_Plan.md` |
| C - SQL script | `sql/RaceDay.sql` |

### Deliberate differences between ERD, SQL script and EF model
* The SQL script adds `CHECK` constraints (`EventType`, `CategoryType`, `DistanceKm > 0`, `Position > 0`) and `DEFAULT SYSUTCDATETIME()` for the audit columns. EF Core (code-first) does not generate these; the API validates the same rules in code.
* Everything else (tables, columns, keys, unique constraints, delete behaviour) is identical.
* No other deviations from the endpoint plan: all 22 planned endpoints are implemented with the same routes and roles.

## Part 2 - The API

### Roles
* **Organiser** - creates/updates/deletes their own events, defines categories, views enrolments for their events, captures results.
* **Participant** - views events and categories, enrols in an event by choosing a category, views/withdraws their own enrolments, views their own results.
* Both roles can register, log in/out, and view/update their own profile.

**Role selection on registration:** anyone can register as a Participant. Registering as an Organiser also requires a server-side *organiser code* (`Registration:OrganiserCode` in `appsettings.json`, default `RACEDAY-ORG-2026`), so users cannot simply promote themselves.

### Authentication / sessions
* Passwords are hashed with PBKDF2-SHA256 (100k iterations, random salt per user) - plain passwords are never stored.
* `POST /api/auth/login` starts a session: a signed, HttpOnly auth cookie carries the user id and role claims, and the role/user id are also stored in the server-side ASP.NET Core session. `POST /api/auth/logout` destroys both.
* No session -> `401`. Wrong role (or someone else's event) -> `403`.




