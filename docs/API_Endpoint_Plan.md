# RaceDay - API Endpoint Plan (Part 1, Section B)

Authentication is session-based (HttpOnly cookie issued by `POST /api/auth/login`).
Roles: **Organiser**, **Participant**. "Any" = any logged-in user. "Public" = no login needed.
Error responses use RFC 7807 problem details (`{ "title", "status", "detail" }`).

| # | HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|---|
| 1 | POST | `/api/auth/register` | Register an account and choose a role. Organiser registration needs the server's organiser code. | Public | `{ firstName, lastName, email, password, dateOfBirth, phone?, role, organiserCode? }` | `201` UserDto; `400` invalid; `403` bad organiser code; `409` email exists |
| 2 | POST | `/api/auth/login` | Log in, start session (cookie) | Public | `{ email, password }` | `200` UserDto + cookie; `401` invalid credentials |
| 3 | POST | `/api/auth/logout` | End the session | Any | - | `204`; `401` |
| 4 | GET | `/api/profile` | View own profile | Any | - | `200` UserDto; `401` |
| 5 | PUT | `/api/profile` | Update own profile (optionally password) | Any | `{ firstName, lastName, dateOfBirth, phone?, newPassword? }` | `200` UserDto; `400`; `401` |
| 6 | GET | `/api/events` | List events (optional `?type=Run\|Walk\|Cycle`) | Any | - | `200` EventDto[]; `401` |
| 7 | GET | `/api/events/{id}` | Get one event | Any | - | `200` EventDto; `404` |
| 8 | POST | `/api/events` | Create an event | Organiser | `{ name, description, eventDate, location, distanceKm, eventType }` | `201` EventDto; `400`; `403` |
| 9 | PUT | `/api/events/{id}` | Update own event | Organiser (owner) | same as #8 | `200` EventDto; `400`; `403`; `404` |
| 10 | DELETE | `/api/events/{id}` | Delete own event (cascades categories, enrolments, results) | Organiser (owner) | - | `204`; `403`; `404` |
| 11 | GET | `/api/events/{eventId}/categories` | List categories of an event | Any | - | `200` CategoryDto[]; `404` |
| 12 | POST | `/api/events/{eventId}/categories` | Add an age/distance category | Organiser (owner) | `{ name, categoryType, minAge?, maxAge?, distanceKm? }` | `201` CategoryDto; `400`; `403`; `404` |
| 13 | PUT | `/api/categories/{id}` | Update a category | Organiser (owner) | same as #12 | `200` CategoryDto; `400`; `403`; `404` |
| 14 | DELETE | `/api/categories/{id}` | Delete a category (not if enrolments exist) | Organiser (owner) | - | `204`; `403`; `404`; `409` |
| 15 | POST | `/api/enrolments` | Enter an event by selecting a category | Participant | `{ eventId, categoryId }` | `201` EnrolmentDto; `400`; `403`; `404`; `409` already enrolled |
| 16 | GET | `/api/enrolments/my` | View own enrolments | Participant | - | `200` EnrolmentDto[]; `403` |
| 17 | DELETE | `/api/enrolments/{id}` | Withdraw own enrolment (not once a result exists) | Participant (owner) | - | `204`; `403`; `404`; `409` |
| 18 | GET | `/api/events/{eventId}/enrolments` | View all enrolments for own event | Organiser (owner) | - | `200` EnrolmentDto[]; `403`; `404` |
| 19 | POST | `/api/results` | Capture finish time and position for an enrolment | Organiser (owner of event) | `{ enrolmentId, finishTime "hh:mm:ss", position }` | `201` ResultDto; `400`; `403`; `404`; `409` |
| 20 | PUT | `/api/results/{id}` | Correct a result | Organiser (owner of event) | `{ finishTime, position }` | `200` ResultDto; `400`; `403`; `404` |
| 21 | GET | `/api/events/{eventId}/results` | View all results for own event (by position) | Organiser (owner) | - | `200` ResultDto[]; `403`; `404` |
| 22 | GET | `/api/results/my` | View own results | Participant | - | `200` ResultDto[]; `403` |

All "Any/Organiser/Participant" endpoints return `401` when no session exists and `403` when the role is wrong.
