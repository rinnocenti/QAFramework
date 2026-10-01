# RESET-035-B — Integration QA

## Coverage added

- QA-NEW-002 now authors one Activity-owned Resettable scene per Activity definition. Its certification checks the Activity owner identity/scope, a valid runtime subject id, and one registered capability for A and B.
- QA-NEW-002 holds B at the existing `WaitVisible` readiness gate after commit, then checks that A has been released and B remains registered. It first submits B with a deliberately invalid required Activity contribution; the Activity transaction registers B's materialized Resettable before validating the invalid binding, then fails before commit. The scenario captures B's Resettable during scene load and checks rollback evidence after the failed request, alongside A's unchanged active state and owner registration.
- QA-NEW-003 reuses the existing Route A/B primary scenes and replacement cycle. It checks the matching Route owner, valid subject id and one capability, and verifies that replacing a Route clears the previous Resettable registration while retaining the new Route registration.

## Evidence boundary

The scenarios use the public evidence exposed by `Resettable`. They prove registration and cleared registration evidence on the authored content units, but do not enumerate the internal registry's complete owner buckets. Owner release idempotency remains a focused helper-level test; the Route replacement scenario covers owner isolation through the public lifecycle.

## Execution status

The scenarios were edited but have not been executed in Unity. A static `dotnet build` was unavailable because no .NET SDK is installed in this environment. Run QA-NEW-002 and QA-NEW-003 in Unity Play Mode and inspect their certification logs before treating RESET-035-B as validated.
