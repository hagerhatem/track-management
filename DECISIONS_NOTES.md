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