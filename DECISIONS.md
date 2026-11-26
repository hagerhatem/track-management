# DECISIONS

This file answers the three questions of the task. More detailed per-phase notes are in `DECISIONS_NOTES.md`.

## 1. What did AI generate, and what did I write or modify myself?

**AI generated:** nearly all of the code. The solution structure (Domain, Application, Infrastructure, Api), entities and EF Core configurations, the services and validators, the controllers, JWT setup, the exception middleware, the seeder, the CORS and security headers code, and the Angular components, service and styles. It also wrote the README and the phase notes.

**What I did myself:**
- Chose the stack and split the project into phases, and defined the business rules (unique ISRC, status workflow, seed data rules).
- Ran every command by hand, and fixed terminal problems (broken pastes, empty files, wrong paths).
- Reviewed the migration and the SQL that EF executed, and tested the database constraints in SQL Server.
- Tested the API with curl and sqlcmd, and the UI in the browser.
- Decided to require the Admin role on the protected endpoints.
- Created a separate database to test the seeder, and decided to fix the Draft to Submitted gap.

I did not write the application code line by line; my role was directing the AI, running, reviewing and testing the result.

## 2. What security issues did I find or introduce, and how did I handle them?

- **Role never checked:** the token carried an Admin role but nothing enforced it, so any valid token could distribute or change status. Fixed with `[Authorize(Roles = "Admin")]` on both protected endpoints.
- **Wildcard AllowedHosts:** it accepted any Host header. Set to `localhost`; other environments must set it explicitly.
- **Default connection string in the base appsettings:** moved to `appsettings.Development.json`, so other environments fail at startup instead of silently using LocalDB.
- **No hardening headers or HSTS:** added a middleware with X-Content-Type-Options, X-Frame-Options, Referrer-Policy and Cache-Control no-store, and HSTS outside Development.
- **Information leaks:** 500 responses are generic, and model-binding errors are sanitized so serializer text is not returned.
- **CORS:** only the configured origin is allowed (never any origin), GET only, and startup fails if no explicit origin is configured.
- **Secrets:** kept in user-secrets. I searched the code and the git history and found no committed secrets. The demo password I used locally is weak and exists only in my user-secrets.
- **Open risks I accepted:** no rate limiting on the dev token endpoint (Development only), the UI has no login, there is no pagination, and I did not test a valid token without the Admin role (403).

## 3. One thing the AI got wrong that I had to fix

**The status workflow allowed an inconsistent state.** The AI wrote the status endpoint so that `PATCH /status` accepted Draft to Submitted with no condition. The business rule I defined says a Submitted track must have at least one distribution. The AI never checked this, and its own test plan predicted the opposite: it said such a request on a Draft track without distributions would return 409.

I found it by testing. The request returned 200 and left track 6 as Submitted with no distributions in the seed database, which broke the rule. I reverted the data through the API, re-ran my SQL rule checks, then fixed the service: Draft to Submitted is now only possible through the `distribute` endpoint, which creates the distributions in the same save. The error message and the comment at the top of the service were updated too.

Verified after the fix: PATCH Draft to Submitted returns 409, `distribute` on a Draft track still returns 200 and makes it Submitted, and after dropping and re-seeding the database the rule queries return 0 rows. Not re-tested: Submitted to Distributed and Submitted to Draft.

**A second, practical problem:** four Auth files (`JwtOptions`, `DemoCredentialsOptions`, `ITokenService`, `TokenRequest`) showed up as modified with 0 lines after a broken paste in my terminal. I restored them with `git checkout` from the last commit and the build passed. I did not confirm the exact cause.
