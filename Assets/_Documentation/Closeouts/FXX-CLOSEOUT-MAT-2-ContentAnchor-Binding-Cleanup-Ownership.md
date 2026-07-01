# FXX-CLOSEOUT - MAT-2 ContentAnchor Binding Cleanup Ownership

Status: Closed / MAT-2 implemented
Date: 2026-06-30

## 1. Decision

MAT-2 is closed as a narrow mechanical cleanup cut.

The duplicated `previous owner -> skip/invoke -> unbind runtime owner` sequence is now owned by one internal helper:

- `ContentAnchorBindingCleanup`

Route and Activity continue to own the semantic decision of when cleanup happens.
ContentAnchor owns the mechanical cleanup execution and the preserved cleanup result.

## 2. What changed

### New helper

- `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorBindingCleanup.cs`

### Updated call sites

- `Packages/com.immersive.framework/Runtime/RouteLifecycle/RouteLifecycleRuntime.cs`
- `Packages/com.immersive.framework/Runtime/ActivityFlow/ActivityFlowRuntime.cs`

The helper now centralizes:

1. validation of the previous owner input;
2. skip logic when there is no previous owner or the owner did not change;
3. invocation of `RuntimeContentAnchorBinding.UnbindRuntimeOwner(...)`;
4. preservation of the existing `ContentAnchorBindingLifecycleResult`.

## 3. Preserved diagnostics

The following diagnostics remain supported by the existing runtime/result surfaces:

- `routeContentAnchorBindingCleanup`
- `routeContentAnchorBindingCleanupRemoved`
- `activityContentAnchorBindingCleanup`
- `activityContentAnchorBindingCleanupRemoved`
- `contentAnchorBindings`

No diagnostic text, count shape or status vocabulary was rewritten to normalize drift.

## 4. Boundary preserved

- No public API was added or changed.
- No enum values were changed.
- No `asmdef` or `package.json` was changed.
- No scene, prefab or asset was changed.
- No scene composition, content dispatch/apply, readiness, ledger or progress code was touched.
- No lifecycle layer was created.
- No fallback or service locator was added.

## 5. What was not changed

- `RouteLifecycleRuntime` remains the semantic owner of Route transition decisions.
- `ActivityFlowRuntime` remains the semantic owner of Activity transition decisions.
- `RuntimeContentAnchorBinding` remains the mechanical binding registry owner.
- Composite lifecycle release binding cleanup was not rewritten in this cut because it is host-mediated and would require a broader host-level integration change outside the allowed minimal scope.

## 6. Validation performed

Static review only.

I verified the cleanup call sites and the helper extraction by inspection and text search.

No Unity compile, import, playmode or smoke was run in this turn.

## 7. Manual validation checklist

Run the expected QA validations after Unity import/compile:

1. Unity compile/import
2. Scope Tail Operation Synthetic Smoke
3. Standard Smoke
4. Content Anchor Diagnostics Smoke
5. Activity Content Anchor Diagnostics Smoke
6. Composite Lifecycle Release Smoke
7. Route Release Smoke
8. Activity Content Execution Participant Source Smoke

Also verify:

- route cleanup still reports the same removal counts;
- activity cleanup still reports the same removal counts;
- empty/no-op cleanup still returns the same executed/skipped shape;
- content anchor binding counts still reach the same baseline after route/activity transitions.

## 8. Remaining risk

The composite lifecycle release path still performs owner cleanup through the host boundary.
That is expected for this cut, but it means the route/activity cleanup helper does not yet own every possible binding cleanup entry point.

## 9. SOLID impact

- SRP improved: cleanup skip/invoke logic now has one internal owner.
- OCP preserved: existing callers still use the same public surface.
- LSP preserved: result shape is unchanged.
- ISP preserved: no broad new interface was introduced.
- DIP preserved: Route/Activity depend on the internal helper rather than duplicating the mechanical sequence.
