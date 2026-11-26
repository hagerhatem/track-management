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
EOF


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
- The Production 404 check: <tested / not tested>.
- JwtBearer rejection of bad tokens is not tested until endpoints are protected (Phase 5).

### (d) Things the AI got wrong that I fixed
- I did not specify file paths for the new files; I had to ask where they go.
- The files were first created as empty VS stubs and the pasted code never reached disk, so the build passed while the controller and Program.cs changes were missing (endpoint returned 404). I wrote the files from the terminal instead.
- I saved placeholder text as the demo credentials in user-secrets by mistake and reset them.

### (e) Verification
- `dotnet build` passed. curl: bad credentials 401, empty body 401, good credentials 200 with Bearer token (1 hour), Swagger UI 200, artists endpoint still open.