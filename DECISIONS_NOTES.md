# Running notes (will become DECISIONS.md)

## Q1: What AI generated vs what I wrote/modified
(append per phase)

## Q2: Security issues found/introduced and how I handled them
(append per phase)

## Q3: Things AI got wrong that I fixed
(append per phase, be specific: what, why wrong, how fixed)

## Phase 1

### (a) What was generated
- Domain: `Artist`, `Track`, `Dsp`, `TrackDistribution` entities with navigation properties (both directions), and enums `TrackStatus` and `DistributionStatus`. Domain has no package dependencies.
- Infrastructure: `AppDbContext` (4 DbSets, applies configurations from assembly) and one `IEntityTypeConfiguration` per entity. Unique indexes on Artist.Email, Track.Isrc, Dsp.Name and (TrackId, DspId). Enums stored as strings (max 20). All FKs use `DeleteBehavior.Restrict`.
- `DependencyInjection.AddInfrastructure(IConfiguration)` registers the DbContext with `UseSqlServer` using `DefaultConnection`; it throws at startup if the connection string is missing.
- Api: `Program.cs` calls `AddInfrastructure`; `appsettings.json` has a LocalDB `DefaultConnection`. Removed `WeatherForecast.cs`, `WeatherForecastController.cs`, `TrackManagement.Api.http` (it only called /weatherforecast), and the `Class1.cs` placeholders in Domain and Infrastructure.
- Migration was intentionally not created. All of this was written without access to a .NET SDK or SQL Server, so it has not been compiled or run by the AI; it must be built and the migration inspected by me.

### (b) Security-relevant choices
- The connection string in `appsettings.json` has no password: LocalDB with Windows integrated auth (`Trusted_Connection=True`). A real/different connection string goes in user-secrets or the `ConnectionStrings__DefaultConnection` env var, never in the repo.
- Caveat: user-secrets only load in the Development environment. Outside Development the committed LocalDB default would be used unless the env var is set. Acceptable for this take-home; for a real deployment the default should live in `appsettings.Development.json` only, so a missing production setting fails loudly.
- `TrustServerCertificate=True` disables certificate validation. Fine for LocalDB on a dev machine; must not be used against a remote/production SQL Server.
- Missing connection string fails fast at startup with a clear message instead of a late runtime error.
- FK `Restrict` prevents accidental cascading deletion of artists, tracks or distributions (also avoids SQL Server multiple-cascade-path errors).
- Isrc is mapped as non-Unicode `varchar(12)` with a unique index, which gives a DB-level guarantee against duplicate tracks even if application validation is bypassed.

### (c) Assumptions and trade-offs
- CONTEXT.md does not specify key types or lengths. Assumed: `int` identity PKs (simple; ids are sequential and guessable, which matters only once per-user authorization exists), `DateOnly` for `ReleaseDate`, UTC `DateTime` for `SubmittedAt` (non-nullable, set when the distribution is created).
- Max lengths are my guesses: Artist.Name 200, Email 254, Country 100 (free text, not forced to ISO alpha-2), Track.Title 300, Genre 100, Dsp.Name 100, status columns 20.
- ISRC format (2 letters + 3 alphanumeric + 2 digits + 5 digits) is NOT enforced in the database, only length <= 12 and uniqueness. Format validation is left to the Application layer (FluentValidation). The column is `max 12` as requested, not fixed-length.
- Unique indexes rely on SQL Server's default case-insensitive collation, so `A@x.com` and `a@x.com` collide. ISRC should additionally be normalized to upper case in the Application layer.
- No indexes on Track.Genre / Track.Status yet, even though the list endpoint filters on them; fine for small data, revisit if needed.
- Package versions in the csproj files use floating `9.0.*`; builds are not fully reproducible. Not changed in this phase.
- `Class1.cs` in the Application project was left untouched (outside the scope of this phase).

### (d) Verification results (added after the migration was created and applied)
- `dotnet build` passed with 0 errors after the code was applied. The AI could not compile it itself, so this was the first real compile.
- Correction to (a): the InitialCreate migration was created by me afterwards and committed together with the code.
- Inspected the migration and the SQL EF executed: 4 tables, 4 unique indexes, all FKs `ON DELETE NO ACTION` (Restrict), no Cascade, Isrc `varchar(12)`, Status columns `nvarchar(20)`.
- My user-secrets `DefaultConnection` points to `localhost\SQLEXPRESS`, so it overrides the committed LocalDB default and the database was created there, not on LocalDB. This confirms the override mechanism works; the LocalDB value in appsettings.json is only a fallback for machines without secrets.
- Constraint test (inside a rolled-back transaction, tables were empty afterwards): duplicate Artist.Email, Dsp.Name, Track.Isrc and (TrackId, DspId) were each rejected with error 2601, and deleting an artist that has tracks was rejected with FK error 547.
- The test also confirmed the unique indexes are case-insensitive (`A@TEST.COM` collided with `a@test.com`, `ega012400001` with `EGA012400001`), as noted in (c). Normalization of ISRC to upper case is still to be done in the Application layer.


## Phase 2

### (a) What was generated
- Application: `CreateArtistRequest`, `ArtistResponse`, `IArtistService`, FluentValidation validator (name required, max 200; valid email, max 254; country required, 2 uppercase letters).
- Infrastructure: `ArtistService` implementing `IArtistService` using `AppDbContext`.
- Api: thin `ArtistsController` (POST returns 201 + Location, GET uses `AsNoTracking`), global exception-handling middleware returning generic ProblemDetails, and an invalid-model-state response factory that returns field-level `ValidationProblemDetails`.
- Interfaces live in Application, implementations in Infrastructure, so Application has no EF Core dependency.

### (b) Security-relevant choices
- The global exception middleware returns generic ProblemDetails only: no stack trace, no exception message.
- Malformed JSON and wrong-type bodies return a fixed message under the `body` key. Raw serializer text, type names and request paths are not echoed (verified with curl: malformed JSON and `"name":123` both produce the same generic 400).
- Duplicate email returns 409 with a fixed message and does not return the existing record.
- Validation rejects bad input before it reaches the database; the unique index on Artist.Email from Phase 1 remains the DB-level guarantee.

### (c) Assumptions and trade-offs
- Country is validated as two uppercase letters only; it is not checked against the real ISO 3166 list (e.g. `ZZ` passes). The column (max 100) from Phase 1 is wider than this rule needs.
- Email uniqueness follows SQL Server's case-insensitive collation (confirmed in Phase 1), so `A@x.com` and `a@x.com` conflict.
- Concurrent duplicate-email requests were not tested. If the service does not map the unique-index violation to 409, such a request would return the generic 500 instead.
- No pagination on GET /api/artists.
- Ids are sequential ints, so artist ids are guessable; no authorization exists yet (Phase 4).

### (d) Things the AI got wrong that I fixed
- The first version of the invalid-model-state response was missing `using Microsoft.AspNetCore.Mvc.Infrastructure;` (`ProblemDetailsFactory`), so the build failed with CS0246.
- The first fix was written for PowerShell, but I use Git Bash. A second, `sed`-based fix was then applied on top, which duplicated the `using` line (CS0105 warning). I removed the duplicate by hand.
- A build failed with MSB3027/MSB3021 because the API from a previous `dotnet run` was still running and locking the DLLs. Stopping it fixed the build.

### (e) Verification
- `dotnet build` passed with 0 errors after the fixes.
- Manual curl tests: POST valid returns 201 with `Location: /api/artists/4`; duplicate email returns 409; invalid fields return 400 with errors for name, email and country; GET returns 200; malformed JSON and wrong type return 400 `application/problem+json` with only the `body` error.
- Identity values were consumed by earlier test runs, so the first artist id is not 1.


## Phase 3

### (a) What was generated
- Application: `CreateTrackRequest` (no Status field), `TrackListItemResponse`, `TrackDetailResponse` (+ artist and distribution DTOs), `TrackFilter`, `ITrackService`, `IsrcNormalizer`, `CreateTrackRequestValidator`.
- Infrastructure: `TrackService` (create, filtered list, details). Registered in both DI files.
- Api: thin `TracksController` (POST 201 + Location, GET list with query filters, GET by id).

### (b) Security-relevant choices
- The create DTO has no Status property, so a client-supplied status is ignored; tracks always start as Draft (verified: a request with `"status":"Distributed"` produced a Draft track).
- Track details return artist id, name and country only; the artist email is not exposed.
- ISRC is normalized (trim, remove dashes/spaces, upper case) before validation and the duplicate check, then validated against the CONTEXT.md format.
- The unique-index violation on Isrc (SQL errors 2601/2627) is mapped to 409, so a concurrent duplicate does not become a 500.
- An invalid status filter returns 400 listing the allowed values; numeric strings are rejected with `Enum.IsDefined`.
- List and detail queries use projection with `Select`, so entities are never returned and no extra data is loaded.

### (c) Assumptions and trade-offs
- Release date must be between 1900-01-01 and one year from today. These bounds are my assumption, not from CONTEXT.md.
- Genre is free text (max 100), not a fixed list. The genre filter lowercases both sides so it does not depend on the collation; this prevents index use on Genre (no index exists anyway).
- No pagination on GET /api/tracks.
- The duplicate-ISRC race path was implemented but not tested concurrently.
- Create re-reads the track through GetByIdAsync, which costs one extra query.
- Ids are sequential ints and there is no authorization on these endpoints yet (Phase 4).

### (d) Things the AI got wrong that I fixed
- The first batch of files was given as bash heredocs, which did not work reliably when pasted into my Git Bash; I created the files through VS Code instead.
- The test commands pasted as one block were garbled by the terminal and sent no requests; I ran them from a script file instead.
- One code comment in `TrackService` contained a wrong normalization example and was corrected.

### (e) Verification
- `dotnet build` passed with 0 errors.
- Manual curl tests: create returns 201 with Location and Draft status; ISRC `eg-a01-24-00001` stored as `EGA012400001`; duplicate ISRC 409; unknown artist 404; invalid fields 400 with per-field errors; client-sent status ignored; list, genre (case-insensitive) and artist+status filters return expected rows; `status=banana` returns 400 with allowed values; unknown track id 404.
- Details of an existing track were verified from the POST response (artist without email, empty distributions). The details endpoint with existing distributions was not tested because distribution creation comes in Phase 5.



## Phase 4

### (a) What was generated
- Api: `JwtOptions`, `DemoCredentialsOptions`, `AddJwtAuthentication` extension, `ITokenService`/`TokenService`, `AuthController` (POST /api/auth/token), `OpenApiBearerTransformer`, updated `Program.cs`, `appsettings.Example.json` (placeholders only). Package added: Swashbuckle.AspNetCore.SwaggerUI.

### (b) Security-relevant choices
- No secrets in the repo: Jwt and Demo values live in user-secrets; the committed example file has placeholders only.
- Startup fails if Jwt:Key is missing or shorter than 32 characters, or if Issuer/Audience are missing.
- Token validation: issuer, audience, lifetime, signing key; only HS256 accepted; ClockSkew 30 seconds.
- The token endpoint returns 404 outside Development and compares credentials with SHA-256 + `CryptographicOperations.FixedTimeEquals`, evaluating both fields every time.
- Bad credentials return a generic 401 that does not say which field was wrong.

### (c) Assumptions and trade-offs
- The demo password I chose locally is weak; it only exists in my user-secrets.
- No rate limiting or lockout on the token endpoint (Development only).
- Role claim "Admin" is issued but no policy uses it yet.
- Non-Development environments have no way to get a token; acceptable for this take-home.
- The Production 404 check: tested with ASPNETCORE_ENVIRONMENT=Production, the token endpoint returned 404.
- JwtBearer rejection of bad tokens is not tested until endpoints are protected (Phase 5).

### (d) Things the AI got wrong that I fixed
- I did not specify file paths for the new files; I had to ask where they go.
- The files were first created as empty VS stubs and the pasted code never reached disk, so the build passed while the controller and Program.cs changes were missing (endpoint returned 404). I wrote the files from the terminal instead.
- I saved placeholder text as the demo credentials in user-secrets by mistake and reset them.

### (e) Verification
- `dotnet build` passed. curl: bad credentials 401, empty body 401, good credentials 200 with Bearer token (1 hour), Swagger UI 200, artists endpoint still open.


## Phase 5

### (a) What was generated
- Application: `DistributeTrackRequest`, `UpdateTrackStatusRequest`, `ITrackWorkflowService`.
- Infrastructure: `TrackWorkflowService` with the workflow rules documented in a comment at the top; registered in DI. It is a separate service so `TrackService` was not rewritten.
- Api: `TracksController` got `POST {id}/distribute` and `PATCH {id}/status`, both with `[Authorize]`. The GET endpoints and POST /api/tracks stay public.

### (b) Security-relevant choices
- Both workflow endpoints require a valid JWT. Verified: no token, and a garbage token, both return 401.
- Distribute validates input before touching the database (empty list, duplicates), then existence of track and DSPs, then existing distributions. Nothing is inserted on any failure (verified: a request mixing an existing DSP and a new one returned 409 and the new DSP was not added).
- Track, distributions and the Draft to Submitted change are saved with one SaveChanges. A unique-index violation on (TrackId, DspId) from a concurrent request is mapped to 409.
- The status value is matched by name only, so numeric strings like "1" are rejected with 400.
- Error messages list ids and allowed values only; no internal details.

### (c) Assumptions and trade-offs
- Any valid token is enough; the Admin role is issued but not checked.
- No API endpoint changes a distribution to Live, so Submitted to Distributed cannot be reached through the API alone. I set a distribution to Live with SQL to test it.
- Submitted to Draft is allowed even when distributions already exist.
- Setting the same status again (for example Draft to Draft) returns 409.
- No optimistic concurrency on the track status, so two simultaneous status changes can overwrite each other.
- `GET /api/tracks?status=` is case-insensitive but still accepts numeric strings through a different code path from Phase 3, while the PATCH rejects them. Small inconsistency, not fixed.
- The DSPs were inserted manually with SQL for testing; the Phase 6 seeder must be idempotent (check by name) because DSP names are unique.

### (d) Things the AI got wrong that I fixed
- The test script paused for a manual SQL step, and the command was pasted into the script terminal instead of being run in another one, so tests 19 to 21 gave misleading results and I re-ran them by hand.
- The first test run used DSP ids 1 and 2 that did not exist (the real ids were 3, 4, 5).

### (e) Verification
- `dotnet build` passed.
- curl: 401 without or with a bad token; 400 for empty, duplicate, missing DSP ids and invalid status; 404 for an unknown track; 200 on distribute with Draft becoming Submitted; 409 on an existing DSP with no partial insert; all invalid transitions return 409; Submitted to Distributed returns 409 without a Live distribution and 200 with one; Distributed to Draft returns 409.

## Phase 6

### (a) What was generated
- Infrastructure/Persistence/Seeding: `SeedData` (fixed DSPs, artists, tracks, distributions) and `DbSeeder`. `DbSeeder` is registered as scoped in `DependencyInjection.cs`.
- `Program.cs` calls the seeder at startup only inside the `IsDevelopment()` block. The seeder runs `MigrateAsync` first, then seeds each table only if it is empty.
- Data: 3 DSPs, 4 artists (EG, AE, LB, SA, example.com emails), 10 tracks (Pop, Hip-Hop, Mahraganat, Arabic Pop, Electronic, Shaabi; 4 Distributed, 4 Submitted, 2 Draft), 17 distributions with mixed Pending/Live/Rejected.
- Logging reports counts only.

### (b) Security-relevant choices
- The seeder is not called outside Development. Verified in Production: the API started and no seeding or migration ran.
- No users, passwords or secrets are seeded. All seed values are fixed and public example data (example.com emails).
- The seed data is validated in code before any database access (ISRC format, Draft without distributions, Distributed with a Live distribution), so a bad edit fails loudly at startup.
- All inserts go through one SaveChanges, so a failure leaves no partial data.

### (c) Assumptions and trade-offs
- DSPs and artists are seeded when their table is empty. Tracks and distributions are treated as one unit: they are seeded only if both tables are empty and every seed artist (by email) and DSP (by name) exists. Otherwise they are skipped with a warning, because a partial seed would break the Phase 5 rules.
- A database with manual data therefore gets no tracks seeded. This follows the requirement but means the seeder cannot enrich a partly filled database.
- Two seeded release dates (2026-12-20, 2027-01-15) are in the future relative to the current date (2026-11-26) but within the one-year limit. They are on Draft tracks; one Submitted track (2026-10-30) is in the past.
- SubmittedAt is derived from the release date (14 days before), not from the clock, so reruns on a new database give identical data.
- Two simultaneous first starts could both pass the empty checks; the unique indexes would make one fail. Not tested.
- The seeder uses the same entities as the API and does not go through the services, so the workflow rules are checked by a separate validation method, not by TrackWorkflowService.
- GET track details returns submittedAt without a Z suffix (existing Phase 3 behavior). Not changed.

### (d) Things the AI got wrong that I fixed
- It gave the GET test commands with port 7001 and https, a guess. The API listened on http://localhost:5288, so the first curl calls returned nothing.
- It gave a launchSettings.json path relative to the wrong directory, and a command with a literal PORT placeholder that failed.
- Its first Production test failed at startup because user-secrets are not loaded outside Development (Jwt:Key missing), so it did not prove anything about the seeder. It was repeated with temporary environment variables.
- Its line-count estimates for DbSeeder and Program.cs were lower than the real values (153 and 45); files were correct.
- The long printf command for these notes got a stray command pasted into it and was run twice, which duplicated the Phase 6 block and corrupted one line. I truncated the file back to the end of Phase 5 and re-appended it in short pieces.

### (e) Verification
- `dotnet build` passed with 0 errors and 0 warnings.
- Fresh database TrackManagementSeedTest (created by Migrate): counts 3 DSPs, 4 artists, 10 tracks, 17 distributions; tracks by status Distributed 4, Draft 2, Submitted 4.
- Second run: log showed Added 0 for every table and counts were unchanged.
- sqlcmd rule checks returned 0 rows: Distributed without Live, Draft with distributions, Submitted without distributions, and ISRCs with wrong length, dash, lower case or wrong pattern.
- curl: GET /api/tracks returns the seeded tracks, status=Distributed returns 4, genre=mahraganat (lower case) returns 1, GET /api/tracks/1 returns the artist without email and 3 distributions.
- Production run with temporary environment variables: no seeding log and the database TrackManagementProdCheck was not created.
- Not tested: seeding a database that has some tables filled, concurrent first starts, and the seeder failing mid-way.

## Phase 7

### (a) What was generated
- frontend/track-ui: Angular 21 app (standalone components, no UI library, plain CSS). `environment.ts` with apiUrl, TypeScript interfaces matching the API DTOs, `TrackService` (getTracks with optional status via HttpParams, getTrack), `TrackListComponent` (table, status filter, loading/error/empty states, row click to detail) and `TrackDetailComponent` (track info, distributions table, empty state, 404 message).
- Api: `Cors/FrontendCorsExtensions.cs`, `UseCors` in `Program.cs`, and `Cors:AllowedOrigins` in `appsettings.Development.json`.

### (b) Security-relevant choices
- CORS allows only the origins listed in `Cors:AllowedOrigins` (http://localhost:4200). Wildcards are filtered out and startup fails if no explicit origin is configured. No AllowAnyOrigin.
- The CORS policy allows only the GET method and the Content-Type and Authorization headers, because the UI is read-only.
- The UI has no authentication and never calls the protected endpoints.
- All API text is rendered with Angular interpolation; there is no innerHTML.
- The status filter is sent with HttpParams, not string concatenation. The list component only accepts values from its own status list.
- The UI shows generic error messages and does not display raw API error bodies. The detail page validates the id before calling the API.

### (c) Assumptions and trade-offs
- CORS is configured only in appsettings.Development.json, so outside Development the API fails at startup unless Cors__AllowedOrigins__0 is set. This is intentional (fail loudly) but means Production needs explicit configuration.
- The API URL (http://localhost:5288) is hard-coded in environment.ts; there is no separate production environment file.
- submittedAt arrives from the API without a Z suffix (Phase 3 behavior), so the date pipe shows it as local time. Not fixed.
- `npm install` failed with an npm internal error (edgesOut) in this project. `--legacy-peer-deps` fixed it and is stored in frontend/track-ui/.npmrc. The root cause was not found; I suspected the spaces in the project path, which I did not confirm.
- Angular CLI generated app.spec.ts tested the placeholder page; it was replaced by a minimal creation test. No unit tests were run.
- No pagination on the list, same as the API.

### (d) Things the AI got wrong that I fixed
- It named the CORS extension class CorsServiceCollectionExtensions, which clashes with a class in ASP.NET, causing build error CS0104. I renamed it to FrontendCorsExtensions.
- It first gave a mkdir for the Cors folder after the command that writes into it, so the first write failed.
- It guessed that the npm error came from the project path, and the first two suggested fixes did not work; only --legacy-peer-deps did.
- Four Auth files (JwtOptions, DemoCredentialsOptions, ITokenService, TokenRequest) showed as modified with 0 lines after a bad paste into the terminal. I restored them with git checkout from the last commit. The exact cause is not confirmed.
- Its line-count estimates for several files were lower than the real counts; the files were correct.
- The API was started with the wrong data: the user-secrets still pointed to the database with manual test data, so the UI showed those rows instead of the seed data. I switched the connection string to the seeded database.

### (e) Verification
- `dotnet build` passed with 0 errors and 0 warnings. `ng build` passed.
- UI list with the seeded database showed all 10 seeded tracks.
- Manual browser checks (done by me, results reported as passing): status filter, detail page of a distributed track with its distributions, a Draft track showing the empty distributions message, and /tracks/999 showing the not found message.
- curl: request with Origin http://localhost:4200 returned 200 with Access-Control-Allow-Origin http://localhost:4200; request with Origin http://evil.example returned 200 without any access-control header.
- Not tested: Production CORS startup failure (missing Cors:AllowedOrigins) and unit tests.
