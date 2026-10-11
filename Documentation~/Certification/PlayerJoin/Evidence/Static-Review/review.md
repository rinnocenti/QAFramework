# Static review — Player Join / rollback

Review status: **performed statically; Unity Test Runner result is user-reported, not agent-executed**.

- Owner routing: bridge tests and canonical Framework matrix belong to `com.immersive.framework`; user-run integration evidence belongs to `QAFramework`.
- Runtime: one internal catch path now forwards the exact callback `PlayerInput` retained by `PendingLocalPlayerJoin` to failure compensation. No public API, status attribute, version or dependency changed.
- Test seam: package test assembly has `InternalsVisibleTo`; bridge accepts `ILocalPlayerProvisioningBackend`; fake implements that existing seam.
- Test isolation: each fixture creates a ScriptableObject Slot profile, PlayerInput host prefab, Session parent, and fake provisioned inputs; teardown destroys created Unity objects and disposes the bridge.
- Nine NUnit tests cover closed Joining, backend exception after reserve, callback-then-exception, destroyed PlayerInput fake-null, null return plus recovery Join, callback mismatch, missing Host, invalid Host configuration, and Host commit failure after Session commit. The backend-exception test also reports `FailedRollback` when a synthetic competing Session mutation consumes the reservation token.
- Direct assembly references: test asmdef includes `Immersive.Framework.Runtime`, `Unity.InputSystem`, and the Unity Test Runner assemblies. Runtime references are not assumed to flow transitively.
- Coverage boundary: fake backend compensation calls and Session snapshots are asserted; real PlayerInputManager behavior, physical destruction timing, Session commit failure and physical compensation failure are not certified. `FailedRollback` coverage is a stale-token recovery failure, not physical cleanup failure.
- User-reported NUnit result after the Runtime correction: 229/229 PASS, PlayerParticipation 38/38, Join contract 26/26 and bridge rollback 9/9. Capture source is this conversation; execution SHA/timestamp and local original artifact are not associated.

`git diff --check` was run in both owning repositories with no whitespace errors. The working copies use LF while Git reports CRLF normalization warnings for existing text attributes; no conversion was forced. NUnit result remains user-reported; the Slice 1 Play Mode run still needs replay after the Runtime correction.
