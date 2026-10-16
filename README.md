# RaceDay — Part 2: RESTful API Development

**Student-level project guide | ASP.NET Core Web API | C# | Entity Framework Core | SQL Server**

## 1. What this project does

RaceDay is the backend for a running, walking and cycling event system. It exposes HTTP endpoints that can be tested in Swagger. It stores information in SQL Server and has two roles:

- **Organiser:** creates and manages their own events, adds categories, views enrolments and records results.
- **Participant:** views events, enrols in an event, manages their own profile and views their own results.

This is the API for Part 2, not a finished public-facing website. A website or mobile app could call these endpoints later.

## 2. Project structure

```text
RaceDay-Part2/
├── RaceDay.sln                     Visual Studio solution
├── src/RaceDay.Api/                The API application
│   ├── Controllers/                Endpoints and request handling
│   ├── Data/RaceDayContext.cs      EF Core database configuration
│   ├── Models/                     Database models and request objects
│   ├── Properties/launchSettings.json
│   ├── Program.cs                  Application setup and middleware
│   └── appsettings.json            Database connection string
├── tests/RaceDay.Tests/            Automated tests
├── database/Part2-schema.sql       Reference SQL schema
├── docs/                           Student guides and submission checklist
└── .github/workflows/dotnet.yml    GitHub Actions build/test workflow
```

## 3. Software you need

1. Windows 10/11.
2. Visual Studio 2022 with the **ASP.NET and web development** workload.
3. The **.NET 8 SDK**.
4. SQL Server LocalDB (often installed with Visual Studio) or another SQL Server instance.
5. A GitHub account for the CI/CD requirement.

Downloads:
- Visual Studio: https://visualstudio.microsoft.com/vs/
- .NET 8 SDK: https://dotnet.microsoft.com/en-us/download/dotnet/8.0
- SQL Server LocalDB information: https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb

## 4. Open and run the project

### Step A — Extract and open

1. Extract the ZIP to a normal folder, for example `Documents\RaceDay-Part2`. Do not run the project from inside the ZIP.
2. Double-click `RaceDay.sln` to open it in Visual Studio 2022.
3. In Solution Explorer, confirm that `RaceDay.Api` is the startup project. If it is not, right-click `RaceDay.Api` and choose **Set as Startup Project**.

### Step B — Check the database connection

Open `src/RaceDay.Api/appsettings.json`. The default connection string is for Windows LocalDB:

```text
Server=(localdb)\MSSQLLocalDB;Database=RaceDayDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

If your SQL Server has a different instance name, change the connection string to match your own installation. LocalDB is Windows-specific.

### Step C — Open the terminal

In Visual Studio choose **View → Terminal**. Change directory to the folder containing `RaceDay.sln`. The terminal should be at the `RaceDay-Part2` project root, not inside `src/RaceDay.Api`.

Restore the project packages:

```bash
dotnet restore RaceDay.sln
```

### Step D — Create the database

Install the Entity Framework command-line tool once, if it is not already installed:

```bash
dotnet tool install --global dotnet-ef
```

If you already have it, update it instead:

```bash
dotnet tool update --global dotnet-ef
```

Create the first migration (a migration is EF Core's record of how to create/update database tables):

```bash
dotnet ef migrations add InitialCreate --project src/RaceDay.Api --startup-project src/RaceDay.Api
```

Then create/update the database:

```bash
dotnet ef database update --project src/RaceDay.Api --startup-project src/RaceDay.Api
```

If an `InitialCreate` migration already exists, **do not create it a second time**. Just run the `database update` command. The provided SQL file is a reference; compare it with your approved Part 1 ERD and SQL script because your lecturer requires the same schema to be carried forward.

### Step E — Start the API

From the solution root run:

```bash
dotnet run --project src/RaceDay.Api
```

The included development launch profile uses `https://localhost:7041` and `http://localhost:5041`. Visual Studio may open Swagger automatically. Otherwise open:

- https://localhost:7041/swagger
- or http://localhost:5041/swagger

Use the actual address shown in your terminal if it differs. Keep the terminal open while testing; closing it stops the API.

## 5. First Swagger test

Swagger is an interactive page for sending requests to the API. Expand an endpoint, select **Try it out**, enter the JSON body and select **Execute**.

### Register an organiser

`POST /api/auth/register`

```json
{
  "fullName": "Alex Organiser",
  "email": "organiser@example.com",
  "password": "SafePassword123!",
  "role": "Organiser"
}
```

### Register a participant

Use the same endpoint with a different email and set the role to `Participant`.

```json
{
  "fullName": "Sam Participant",
  "email": "participant@example.com",
  "password": "SafePassword123!",
  "role": "Participant"
}
```

Passwords are hashed before being stored. Use a unique email for each account.

### Log in

`POST /api/auth/login`

```json
{
  "email": "organiser@example.com",
  "password": "SafePassword123!"
}
```

Login creates a session cookie. Later requests must send that cookie. Depending on the browser and Swagger setup, the cookie might not be retained as expected. If a protected endpoint responds with `401 Unauthorized` after login, check whether the session cookie is being sent; do not assume the credentials are wrong.

### Create an event as the organiser

`POST /api/events`

```json
{
  "name": "RaceDay Community 10K",
  "description": "A community road-running event.",
  "eventDate": "2027-03-20T08:00:00Z",
  "location": "Pretoria, South Africa",
  "distanceKm": 10,
  "eventType": "run"
}
```

Supported event types are `run`, `walk` and `cycle`. This request needs an organiser session.

### Add a category

`POST /api/events/1/categories`

Replace `1` with the event ID returned by your create-event request.

```json
{
  "name": "Under 20",
  "minimumAge": 0,
  "maximumAge": 19,
  "distanceKm": 10
}
```

### Enrol as a participant

Log in as the participant and use `POST /api/events/1/enrolments`:

```json
{
  "categoryId": 1
}
```

Use the actual event and category IDs from your responses. A participant can enrol only once per event.

### Record a result as an organiser

`POST /api/events/1/enrolments/1/result`

```json
{
  "finishTime": "00:48:32",
  "finishPosition": 12
}
```

Replace the IDs with the correct event and enrolment IDs. The organiser must own the event.

## 6. Endpoint summary

| Method | Endpoint | Purpose / access |
|---|---|---|
| POST | `/api/auth/register` | Create an Organiser or Participant account |
| POST | `/api/auth/login` | Sign in and start a session |
| POST | `/api/auth/logout` | End the current session |
| GET | `/api/auth/me` | View the current signed-in user |
| GET, PUT | `/api/profile` | View/update your own profile |
| GET | `/api/events` | List events (public) |
| GET | `/api/events/{id}` | View one event (public) |
| POST | `/api/events` | Create an event (Organiser) |
| PUT, DELETE | `/api/events/{id}` | Update/delete your own event (Organiser) |
| GET | `/api/events/{eventId}/categories` | View event categories (public) |
| POST | `/api/events/{eventId}/categories` | Add category (owning Organiser) |
| PUT, DELETE | `/api/events/{eventId}/categories/{categoryId}` | Update/delete category (owning Organiser) |
| POST | `/api/events/{eventId}/enrolments` | Enrol in an event (Participant) |
| GET | `/api/events/{eventId}/enrolments` | View event enrolments (owning Organiser) |
| GET | `/api/my/enrolments` | View your enrolments (Participant) |
| DELETE | `/api/enrolments/{id}` | Cancel your own enrolment (Participant) |
| POST | `/api/events/{eventId}/enrolments/{enrolmentId}/result` | Capture result (owning Organiser) |
| GET | `/api/my/results` | View your results (Participant) |
| GET | `/api/events/{eventId}/results` | View event results (owning Organiser) |

## 7. Run the tests

From the solution root:

```bash
dotnet test RaceDay.sln
```

The current test project contains starter unit tests for password hashing, allowed role names, event types and distance validation. These tests are useful but **do not cover every API endpoint**. Before submission, expand the tests to cover registration/login success and failure, unauthenticated access, role restrictions, event management and enrolment recording. Do not report tests as passing unless you have run them and seen the results.

## 8. GitHub Actions (CI/CD)

A workflow is included at `.github/workflows/dotnet.yml`. It restores packages, builds the solution and runs tests when code is pushed to the configured branches or a pull request is opened.

1. Create a GitHub repository under your own account.
2. Push this project to the repository, using a clearly named `part-2` branch or folder.
3. Open the **Actions** tab in GitHub and select the latest workflow run.
4. Fix any errors and push again until the run succeeds.
5. Take your own screenshot of the successful green workflow run and add it to the README (for example `docs/ci-success.png`).

Do not create a fake screenshot or claim a workflow succeeded before GitHub shows a successful run.

## 9. Submission checklist

- [ ] The API builds and runs on your computer.
- [ ] The database schema has been checked against the approved Part 1 ERD and SQL script.
- [ ] Swagger shows the implemented endpoints and you have tested them.
- [ ] Unit tests have been run; add more tests for required success and failure cases.
- [ ] The code has been pushed to your own GitHub repository.
- [ ] At least 20 meaningful commits have been made over the development process, as required by the brief. Do not fabricate or backdate commits.
- [ ] GitHub Actions builds and tests successfully.
- [ ] A real screenshot of the successful CI run is in the README.
- [ ] An unlisted YouTube video demonstrates the API, code structure, authentication, endpoints and tests.
- [ ] The GitHub repository link is submitted on ARC.
- [ ] The unlisted YouTube video link is submitted and included in the README.

**YouTube link to add after recording:** `PASTE_YOUR_UNLISTED_YOUTUBE_URL_HERE`

## 10. Academic note

Use this project as a learning starting point. Read each controller and model, run the endpoints yourself, and make sure you can explain how the code works. Adapt the database to your approved Part 1 design and follow your institution's rules about permitted assistance and acknowledgement. No build, test run, GitHub workflow, screenshot or video is claimed as completed by this ZIP.
