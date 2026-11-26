# Project: Track Management API (Takwene take-home)

## What this project is
A backend + frontend for a MENA media-tech company that distributes music content to DSPs
(Digital Service Providers: Spotify, Apple Music, YouTube). It manages artists, their tracks,
and the distribution of each track to one or more DSPs.

## Stack
- Backend: .NET 8 Web API, EF Core + SQL Server, FluentValidation, JWT Bearer
- Frontend: Angular (standalone components) in /frontend/track-ui
- Structure: src/TrackManagement.{Domain,Application,Infrastructure,Api}

## Architecture rules (Clean Architecture)
- Domain: entities + enums only, zero dependencies
- Application: DTOs, interfaces, services, validators. References Domain only
- Infrastructure: DbContext, EF configurations, migrations, seeder, service/repo implementations
- Api: thin controllers, DI setup, middleware, JWT config, CORS
- Controllers never touch DbContext directly
- Never return entities from the API, always DTOs
- Enums stored as strings in the DB
- Secrets are never hardcoded or committed: use dotnet user-secrets / env vars
- FK delete behavior = Restrict (also avoids SQL Server multiple cascade path errors)

## Entities
- Artist(Id, Name, Email UNIQUE, Country)
- Track(Id, Title, ArtistId, Isrc UNIQUE, ReleaseDate, Genre, Status: Draft/Submitted/Distributed)
- Dsp(Id, Name UNIQUE)
- TrackDistribution(Id, TrackId, DspId, SubmittedAt, Status: Pending/Live/Rejected), UNIQUE(TrackId, DspId)

## Endpoints
- POST /api/artists | GET /api/artists
- POST /api/tracks | GET /api/tracks?artistId=&genre=&status= | GET /api/tracks/{id} (with distributions)
- POST /api/tracks/{id}/distribute  [JWT]
- PATCH /api/tracks/{id}/status     [JWT]
- POST /api/auth/token (dev-only demo token)

## Conventions
- Errors: ProblemDetails (RFC 7807). Validation errors -> 400 with per-field messages.
  Duplicates -> 409. Not found -> 404.
- ISRC format: 12 chars = 2 letters country + 3 alphanumeric registrant + 2 digits year + 5 digits
  (e.g. EGA012400001)
- Small commits with conventional messages (feat:, fix:, chore:, docs:)
- After every phase: update PROGRESS.md, append to DECISIONS_NOTES.md, commit.

## Deliverables (for submission)
Public GitHub repo with many commits, README.md (setup, migrations, how to get JWT),
DECISIONS.md (3 questions: what AI wrote vs me, security issues, one AI mistake I fixed).