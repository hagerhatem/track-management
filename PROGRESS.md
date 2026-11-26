# Progress

- [x] Phase 0: scaffold solution + context files
- [x] Phase 1: Domain + EF Core (SQL Server) + initial migration
- [x] Phase 2: Artists endpoints + validation + global error handling
- [x] Phase 3: Tracks endpoints (create, list+filters, details)
- [x] Phase 4: JWT auth + dev token endpoint
- [x] Phase 5: Distribute + Status endpoints (protected)
- [x] Phase 6: Seed data
- [ ] Phase 7: Angular (list + detail) + CORS
- [ ] Phase 8: Security review + fixes
- [ ] Phase 9: README + DECISIONS.md
- [ ] Phase 10: Final check + submission

## Last completed
Phase 6: idempotent Development-only seeding (DbSeeder). Runs Migrate(), then seeds each table only if empty, with a single SaveChanges. Seeds 3 DSPs, 4 artists, 10 tracks and 17 distributions with fixed emails and ISRCs. Verified on a fresh database: counts, second run adds nothing, Phase 5 rules hold, no seeding in Production.

## Next step
Phase 7: Angular list + detail + CORS.
