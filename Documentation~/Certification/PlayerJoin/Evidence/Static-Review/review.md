# Static review — Player Join / rollback

Review status: **performed statically; new NUnit tests NOT RUN**.

- Owner routing: bridge tests and canonical Framework matrix belong to `com.immersive.framework`; user-run integration evidence belongs to `QAFramework`.
- Runtime: no Runtime source, public API, status attribute, version or dependency was changed.
- Test seam: package test assembly has `InternalsVisibleTo`; bridge accepts `ILocalPlayerProvisioningBackend`; fake implements that existing seam.
- Test isolation: each fixture creates a ScriptableObject Slot profile, PlayerInput host prefab, Session parent, and fake provisioned inputs; teardown destroys created Unity objects and disposes the bridge.
- Nine named NUnit tests cover closed Joining, backend exception after reserve, callback-then-exception, destroyed PlayerInput fake-null, null return plus recovery Join, callback mismatch, missing Host, invalid Host configuration, and Host commit failure after Session commit.
- Direct assembly references: test asmdef includes `Immersive.Framework.Runtime`, `Unity.InputSystem`, and the Unity Test Runner assemblies. Runtime references are not assumed to flow transitively.
- Coverage boundary: fake backend compensation calls and Session snapshots are asserted; real PlayerInputManager behavior, destruction timing, commit failure and rollback failure are not certified by these tests.
- User-observed NUnit result: 220/0 suite, PlayerParticipation 29/29, Join contract 26/26; this predates the newly added bridge rollback tests and is recorded separately in the NUnit evidence manifest.

`git diff --check` was run in both owning repositories with no whitespace errors. The working copies use LF while Git reports CRLF normalization warnings for existing text attributes; no conversion was forced. Unity Test Runner remains a manual validation step.
