/* =====================================================================
   RaceDay - Part 1, Section C: SQL Server schema + sample data
   Run in SSMS. Matches the ERD in /docs/RaceDay_ERD.png and the EF Core model
   in /src/RaceDay.Api/Models/Entities.cs.
   Sample login password for every seeded user:  Password123!
   ===================================================================== */

IF DB_ID(N'RaceDayDb') IS NULL
    CREATE DATABASE RaceDayDb;
GO
USE RaceDayDb;
GO

/* ---------- Drop (child -> parent) so the script can be re-run ---------- */
DROP TABLE IF EXISTS dbo.Results;
DROP TABLE IF EXISTS dbo.Enrolments;
DROP TABLE IF EXISTS dbo.Categories;
DROP TABLE IF EXISTS dbo.Events;
DROP TABLE IF EXISTS dbo.Users;
DROP TABLE IF EXISTS dbo.Roles;
GO

/* ---------- Tables ---------- */
CREATE TABLE dbo.Roles (
    RoleId    INT IDENTITY(1,1) NOT NULL,
    RoleName  NVARCHAR(30)      NOT NULL,
    CONSTRAINT PK_Roles PRIMARY KEY (RoleId),
    CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName)
);

CREATE TABLE dbo.Users (
    UserId        INT IDENTITY(1,1) NOT NULL,
    FirstName     NVARCHAR(100)     NOT NULL,
    LastName      NVARCHAR(100)     NOT NULL,
    Email         NVARCHAR(200)     NOT NULL,
    PasswordHash  NVARCHAR(500)     NOT NULL,   -- PBKDF2-SHA256, never plain text
    DateOfBirth   DATETIME2         NOT NULL,
    Phone         NVARCHAR(30)      NULL,
    RoleId        INT               NOT NULL,
    CreatedAt     DATETIME2         NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (RoleId)
);

CREATE TABLE dbo.Events (
    EventId      INT IDENTITY(1,1) NOT NULL,
    Name         NVARCHAR(200)     NOT NULL,
    Description  NVARCHAR(2000)    NOT NULL,
    EventDate    DATETIME2         NOT NULL,
    Location     NVARCHAR(300)     NOT NULL,
    DistanceKm   DECIMAL(6,2)      NOT NULL,
    EventType    NVARCHAR(10)      NOT NULL,   -- Run | Walk | Cycle
    OrganiserId  INT               NOT NULL,
    CreatedAt    DATETIME2         NOT NULL CONSTRAINT DF_Events_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Events PRIMARY KEY (EventId),
    CONSTRAINT FK_Events_Users_Organiser FOREIGN KEY (OrganiserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Events_EventType CHECK (EventType IN (N'Run', N'Walk', N'Cycle')),
    CONSTRAINT CK_Events_DistanceKm CHECK (DistanceKm > 0)
);

CREATE TABLE dbo.Categories (
    CategoryId    INT IDENTITY(1,1) NOT NULL,
    EventId       INT               NOT NULL,
    Name          NVARCHAR(100)     NOT NULL,
    CategoryType  NVARCHAR(10)      NOT NULL,  -- Age | Distance
    MinAge        INT               NULL,
    MaxAge        INT               NULL,
    DistanceKm    DECIMAL(6,2)      NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId) REFERENCES dbo.Events (EventId) ON DELETE CASCADE,
    CONSTRAINT CK_Categories_CategoryType CHECK (CategoryType IN (N'Age', N'Distance'))
);

CREATE TABLE dbo.Enrolments (
    EnrolmentId  INT IDENTITY(1,1) NOT NULL,
    UserId       INT               NOT NULL,   -- the Participant
    EventId      INT               NOT NULL,
    CategoryId   INT               NOT NULL,
    EnrolledAt   DATETIME2         NOT NULL CONSTRAINT DF_Enrolments_EnrolledAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Enrolments PRIMARY KEY (EnrolmentId),
    CONSTRAINT UQ_Enrolments_User_Event UNIQUE (UserId, EventId),  -- one entry per participant per event
    CONSTRAINT FK_Enrolments_Users      FOREIGN KEY (UserId)     REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Enrolments_Events     FOREIGN KEY (EventId)    REFERENCES dbo.Events (EventId) ON DELETE CASCADE,
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories (CategoryId)
);

CREATE TABLE dbo.Results (
    ResultId     INT IDENTITY(1,1) NOT NULL,
    EnrolmentId  INT               NOT NULL,
    FinishTime   TIME(7)           NOT NULL,
    Position     INT               NOT NULL,
    RecordedAt   DATETIME2         NOT NULL CONSTRAINT DF_Results_RecordedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Results PRIMARY KEY (ResultId),
    CONSTRAINT UQ_Results_Enrolment UNIQUE (EnrolmentId),          -- 1:1 with Enrolments
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId) REFERENCES dbo.Enrolments (EnrolmentId) ON DELETE CASCADE,
    CONSTRAINT CK_Results_Position CHECK (Position > 0)
);
GO

/* ---------- Sample data ---------- */
SET IDENTITY_INSERT dbo.Roles ON;
INSERT INTO dbo.Roles (RoleId, RoleName) VALUES (1, N'Organiser'), (2, N'Participant');
SET IDENTITY_INSERT dbo.Roles OFF;

INSERT INTO dbo.Users (FirstName, LastName, Email, PasswordHash, DateOfBirth, Phone, RoleId) VALUES
 (N'Olivia', N'Organiser', N'organiser@raceday.test', N'v1.100000.Fo0zC34k63rkkueRI2l6YQ==.TPS4VVhNccMmMSrgEUef2DD9OicCfNFUEu7nObf97f8=', '1985-03-14', N'0821110001', 1),
 (N'Pieter', N'Participant', N'pieter@raceday.test',  N'v1.100000.1cc7x63P2lVJi4NNI/IFHQ==.GcPMfoHE6kOwdeC5HT8IeUZXZkc0St1Jq38/qWL2YsA=', '1998-07-22', N'0821110002', 2),
 (N'Thandi', N'Nkosi',      N'thandi@raceday.test',   N'v1.100000.Vg+JKfn8ovlBg9mYfi+poA==.HhwQ17jjkRwdl4gK+8at48ipoWc85u6EZFZDQUmjnQ4=', '2007-11-05', N'0821110003', 2),
 (N'Sam',    N'Senior',     N'sam@raceday.test',      N'v1.100000.AjrTHMoNp7NCLKE0fK2eOQ==.MaUJI5NIhTvMRo4jL1C4tYXvKI7+/2D5rv3Twnq9tcs=', '1972-01-30', N'0821110004', 2);

INSERT INTO dbo.Events (Name, Description, EventDate, Location, DistanceKm, EventType, OrganiserId) VALUES
 (N'Table Mountain Trail Run', N'Scenic trail run along the slopes of Table Mountain.', '2026-03-15T07:00:00', N'Cape Town', 21.10, N'Run', 1),
 (N'Sunrise Fun Walk',         N'Relaxed community walk along the promenade.',          '2026-11-20T06:30:00', N'Durban',    5.00, N'Walk', 1),
 (N'Winelands Cycle Classic',  N'Road cycle race through the winelands.',               '2026-12-05T06:00:00', N'Stellenbosch', 60.00, N'Cycle', 1);

INSERT INTO dbo.Categories (EventId, Name, CategoryType, MinAge, MaxAge, DistanceKm) VALUES
 (1, N'Under 20', N'Age',      0, 19, NULL),
 (1, N'Senior',   N'Age',      20, 99, NULL),
 (2, N'5km',      N'Distance', NULL, NULL, 5.00),
 (3, N'60km',     N'Distance', NULL, NULL, 60.00);

INSERT INTO dbo.Enrolments (UserId, EventId, CategoryId, EnrolledAt) VALUES
 (2, 1, 2, '2026-02-01T10:00:00'),
 (3, 1, 1, '2026-02-02T11:30:00'),
 (4, 1, 2, '2026-02-03T09:15:00'),
 (2, 3, 4, '2026-10-01T08:00:00');

INSERT INTO dbo.Results (EnrolmentId, FinishTime, Position) VALUES
 (1, '01:52:30', 2),
 (2, '02:05:10', 3),
 (3, '01:48:05', 1);
GO

/* ---------- Quick verification ---------- */
SELECT e.Name AS EventName, c.Name AS Category, u.FirstName + N' ' + u.LastName AS Participant, r.FinishTime, r.Position
FROM dbo.Results r
JOIN dbo.Enrolments en ON en.EnrolmentId = r.EnrolmentId
JOIN dbo.Events e      ON e.EventId = en.EventId
JOIN dbo.Categories c  ON c.CategoryId = en.CategoryId
JOIN dbo.Users u       ON u.UserId = en.UserId
ORDER BY r.Position;
GO
