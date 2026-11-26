# Progress

- [x] Phase 0: scaffold solution + context files
- [x] Phase 1: Domain + EF Core (SQL Server) + initial migration
- [x] Phase 2: Artists endpoints + validation + global error handling
- [x] Phase 3: Tracks endpoints (create, list+filters, details)
- [x] Phase 4: JWT auth + dev token endpoint
- [x] Phase 5: Distribute + Status endpoints (protected)
- [ ] Phase 6: Seed data
- [ ] Phase 7: Angular (list + detail) + CORS
- [ ] Phase 8: Security review + fixes
- [ ] Phase 9: README + DECISIONS.md
- [ ] Phase 10: Final check + submission

## Last completed
Phase 5: POST /api/tracks/{id}/distribute and PATCH /api/tracks/{id}/status, both protected by [Authorize] (any valid token). Distribute is atomic (single SaveChanges), Draft becomes Submitted, 409 lists DSPs that already have a distribution. Status transitions are Draft to Submitted, Submitted to Distributed (needs a Live distribution) and Submitted to Draft; Distributed is final. GET endpoints stay public. Verified with curl.

## Next step
Phase 6: Seed data.
