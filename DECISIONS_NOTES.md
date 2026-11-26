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
