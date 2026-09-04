-- RaceDay Database Schema
-- Drop tables in reverse order if they exist
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.EventEnrolments', 'U') IS NOT NULL DROP TABLE dbo.EventEnrolments;
IF OBJECT_ID('dbo.WeatherData', 'U') IS NOT NULL DROP TABLE dbo.WeatherData;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

-- Create Users Table (Handles both Organisers and Participants)
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Role NVARCHAR(20) NOT NULL DEFAULT 'Participant' CHECK (Role IN ('Organiser', 'Participant')),
    DateRegistered DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

-- Create Categories Table
CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(500) NULL
);
GO

-- Create Events Table
CREATE TABLE Events (
    EventId INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    CategoryId INT NOT NULL FOREIGN KEY REFERENCES Categories(CategoryId),
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    StartDateTime DATETIME2 NOT NULL,
    EndDateTime DATETIME2 NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    MaxParticipants INT NOT NULL DEFAULT 100,
    EntryFee DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    CONSTRAINT CHK_MaxParticipants CHECK (MaxParticipants > 0)
);
GO

-- Create EventEnrolments Table
CREATE TABLE EventEnrolments (
    EnrolmentId INT IDENTITY(1,1) PRIMARY KEY,
    EventId INT NOT NULL FOREIGN KEY REFERENCES Events(EventId),
    ParticipantId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    EnrolmentDateTime DATETIME2 NOT NULL DEFAULT GETDATE(),
    Status NVARCHAR(20) NOT NULL DEFAULT 'Enrolled' CHECK (Status IN ('Enrolled', 'Cancelled', 'Completed')),
    CONSTRAINT UQ_Event_Participant UNIQUE (EventId, ParticipantId)
);
GO

-- Create Results Table
CREATE TABLE Results (
    ResultId INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId INT NOT NULL UNIQUE FOREIGN KEY REFERENCES EventEnrolments(EnrolmentId),
    FinishTime TIME NOT NULL,
    Rank INT NOT NULL,
    PersonalBest BIT NOT NULL DEFAULT 0
);
GO

-- Create WeatherData Table (for race day planning)
CREATE TABLE WeatherData (
    WeatherId INT IDENTITY(1,1) PRIMARY KEY,
    EventId INT NOT NULL FOREIGN KEY REFERENCES Events(EventId),
    ForecastDate DATE NOT NULL,
    TemperatureC DECIMAL(5,2) NOT NULL,
    Conditions NVARCHAR(100) NOT NULL,
    WindSpeedKph DECIMAL(5,2) NOT NULL
);
GO

-- Seed Data
INSERT INTO Users (Email, PasswordHash, FirstName, LastName, Role) VALUES
('thabo@raceday.co.za', 'hashed_pw_1', 'Thabo', 'Mokoena', 'Organiser'),
('lindiwe@raceday.co.za', 'hashed_pw_2', 'Lindiwe', 'Nkosi', 'Organiser'),
('jane@participant.com', 'hashed_pw_3', 'Jane', 'Doe', 'Participant'),
('peter@participant.com', 'hashed_pw_4', 'Peter', 'Smith', 'Participant');
GO

INSERT INTO Categories (Name, Description) VALUES
('Running', 'Road running events and marathons'),
('Walking', 'Community walks and fun runs'),
('Cycling', 'Road cycling races');
GO

INSERT INTO Events (OrganiserId, CategoryId, Name, Description, StartDateTime, EndDateTime, Location, MaxParticipants, EntryFee) VALUES
(1, 1, 'Soweto Marathon', 'The iconic 42km race through Soweto.', '2026-11-01 06:00:00', '2026-11-01 14:00:00', 'Nokia Stadium, Soweto', 5000, 250.00),
(1, 2, 'Durban Fun Walk', 'A relaxed 10km community walk along the beachfront.', '2026-10-15 08:00:00', '2026-10-15 12:00:00', 'North Beach, Durban', 1000, 50.00),
(2, 3, 'Cape Town Cycle Tour', 'The worlds largest timed cycle race.', '2026-03-08 06:15:00', '2026-03-08 16:00:00', 'Cape Town CBD', 35000, 400.00);
GO

INSERT INTO EventEnrolments (EventId, ParticipantId, Status) VALUES
(1, 3, 'Enrolled'),
(1, 4, 'Enrolled'),
(2, 3, 'Enrolled');
GO

INSERT INTO Results (EnrolmentId, FinishTime, Rank, PersonalBest) VALUES
(1, '04:12:45', 523, 0),
(2, '04:45:10', 1124, 0);
GO

INSERT INTO WeatherData (EventId, ForecastDate, TemperatureC, Conditions, WindSpeedKph) VALUES
(1, '2026-11-01', 18.50, 'Partly Cloudy', 12.00),
(2, '2026-10-15', 24.00, 'Sunny', 15.50);
GO
SELECT * FROM Users;
SELECT * FROM Events;