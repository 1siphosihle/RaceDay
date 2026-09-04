# RaceDay API Endpoint Plan

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
| :--- | :--- | :--- | :--- | :--- | :--- |
| POST | /api/auth/register | Registers a new user account in the system. | None (Public) | { "email", "password", "firstName", "lastName", "role" } | 201 Created - User details returned. 400 Bad Request - Email already in use. |
| POST | /api/auth/login | Authenticates a user and returns an access token. | None (Public) | { "email", "password" } | 200 OK - Access token returned. 401 Unauthorized - Invalid credentials. |
| GET | /api/users/profile | Retrieves the currently logged-in user's profile details. | Any (Logged In) | None | 200 OK - User profile object. 404 Not Found - User not found. |
| PUT | /api/users/profile | Updates the current user's name or contact details. | Any (Logged In) | { "firstName", "lastName" } | 200 OK - Updated user object. 400 Bad Request - Validation errors. |
| GET | /api/categories | Lists all event categories (Running, Walking, Cycling). | None (Public) | None | 200 OK - List of categories. |
| GET | /api/events | Fetches a list of upcoming events with optional filtering. | None (Public) | None | 200 OK - List of event objects. |
| POST | /api/events | Creates a new event entry in the system. | Organiser | { "name", "description", "startDateTime", "location", "categoryId", "maxParticipants" } | 201 Created - Event object. 403 Forbidden - Not an Organiser. |
| PUT | /api/events/{id} | Updates an existing event's details. | Organiser | { "name", "description", "startDateTime" } | 200 OK - Updated event. 404 Not Found - Event ID invalid. |
| DELETE | /api/events/{id} | Permanently deletes an event. | Organiser | None | 204 No Content - Successfully deleted. 404 Not Found - Event ID invalid. |
| POST | /api/events/{eventId}/enrol | Enrols the current participant in a specific event. | Participant | None | 201 Created - Enrolment record. 409 Conflict - Already enrolled. |
| GET | /api/enrolments/my | Retrieves a list of the current participant's event enrolments. | Participant | None | 200 OK - List of enrolments with event details. |
| GET | /api/events/{eventId}/enrolments | Retrieves all enrolments for a specific event. | Organiser | None | 200 OK - List of enrolled participants. |
| POST | /api/events/{eventId}/results | Captures a participant's finish time and rank. | Organiser | { "participantId", "finishTime", "rank" } | 201 Created - Result record. 400 Bad Request - Invalid time format. |
| GET | /api/results/my | Retrieves the current participant's personal race history. | Participant | None | 200 OK - List of past results. |
| GET | /api/events/{eventId}/results | Fetches the public results leaderboard for an event. | None (Public) | None | 200 OK - Sorted list of results. |