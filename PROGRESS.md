# Progress

- [x] Phase 0: scaffold solution + context files
- [x] Phase 1: Domain + EF Core (SQL Server) + initial migration
- [x] Phase 2: Artists endpoints + validation + global error handling
- [x] Phase 3: Tracks endpoints (create, list+filters, details)
- [x] Phase 4: JWT auth + dev token endpoint
- [ ] Phase 5: Distribute + Status endpoints (protected)
- [ ] Phase 6: Seed data
- [ ] Phase 7: Angular (list + detail) + CORS
- [ ] Phase 8: Security review + fixes
- [ ] Phase 9: README + DECISIONS.md
- [ ] Phase 10: Final check + submission

## Last completed
Phase 4: JwtBearer (issuer, audience, lifetime, signing key validated; ClockSkew 30s; HS256 only; fail-fast on missing/short Jwt:Key), POST /api/auth/token (Development only, constant-time credential check, 60-minute token with Admin role), OpenAPI Bearer scheme + Swagger UI. No endpoint is protected yet.

## Next step
Phase 5: Distribute + Status endpoints (protected).