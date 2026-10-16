# RaceDay ERD (text version)

The diagram is in `RaceDay_ERD.png`. The same model as a Mermaid diagram:

```mermaid
erDiagram
    ROLES ||--o{ USERS : "has"
    USERS ||--o{ EVENTS : "organises"
    USERS ||--o{ ENROLMENTS : "enrols"
    EVENTS ||--o{ CATEGORIES : "offers"
    EVENTS ||--o{ ENROLMENTS : "receives"
    CATEGORIES ||--o{ ENROLMENTS : "chosen in"
    ENROLMENTS ||--o| RESULTS : "produces"

    ROLES { int RoleId PK
            nvarchar RoleName UK }
    USERS { int UserId PK
            nvarchar FirstName
            nvarchar LastName
            nvarchar Email UK
            nvarchar PasswordHash
            datetime2 DateOfBirth
            nvarchar Phone
            int RoleId FK
            datetime2 CreatedAt }
    EVENTS { int EventId PK
             nvarchar Name
             nvarchar Description
             datetime2 EventDate
             nvarchar Location
             decimal DistanceKm
             nvarchar EventType
             int OrganiserId FK
             datetime2 CreatedAt }
    CATEGORIES { int CategoryId PK
                 int EventId FK
                 nvarchar Name
                 nvarchar CategoryType
                 int MinAge
                 int MaxAge
                 decimal DistanceKm }
    ENROLMENTS { int EnrolmentId PK
                 int UserId FK
                 int EventId FK
                 int CategoryId FK
                 datetime2 EnrolledAt }
    RESULTS { int ResultId PK
              int EnrolmentId FK
              time FinishTime
              int Position
              datetime2 RecordedAt }
```

| Relationship | Cardinality | Notes |
|---|---|---|
| Roles -> Users | one-to-many | A role has many users; each user has one role |
| Users (Organiser) -> Events | one-to-many | `Events.OrganiserId` |
| Events -> Categories | one-to-many | Deleting an event deletes its categories |
| Users (Participant) <-> Events | many-to-many | Resolved by `Enrolments` (UNIQUE `UserId, EventId`) |
| Categories -> Enrolments | one-to-many | The category chosen when entering |
| Enrolments -> Results | one-to-one | `Results.EnrolmentId` is UNIQUE |
