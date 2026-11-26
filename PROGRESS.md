# Progress

- [x] Phase 0: scaffold solution + context files
- [x] Phase 1: Domain + EF Core (SQL Server) + initial migration
- [x] Phase 2: Artists endpoints + validation + global error handling
- [x] Phase 3: Tracks endpoints (create, list+filters, details)
- [ ] Phase 4: JWT auth + dev token endpoint
- [ ] Phase 5: Distribute + Status endpoints (protected)
- [ ] Phase 6: Seed data
- [ ] Phase 7: Angular (list + detail) + CORS
- [ ] Phase 8: Security review + fixes
- [ ] Phase 9: README + DECISIONS.md
- [ ] Phase 10: Final check + submission

## Last completed
Phase 3: POST /api/tracks (201 + Location, Draft only, ISRC normalized, 404 unknown artist, 409 duplicate ISRC incl. unique-index race), GET /api/tracks with artistId/genre/status filters (projection with Select, AsNoTracking), GET /api/tracks/{id} with artist (no email) and distributions. Verified manually with curl.

## Next step
Phase 4: JWT auth + dev token endpoint.