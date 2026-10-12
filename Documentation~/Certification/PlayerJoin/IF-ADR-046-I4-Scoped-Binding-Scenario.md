# IF-ADR-046 I4 — Scoped Join Binding QA scenario proposal

Status: **Proposed; no QA implementation authorized in this task**  
Owner: **QAFramework**  
Framework decision: [IF-ADR-046](../../../../ImmersivePackages/com.immersive.framework/Documentation~/Architecture/ADRs/IF-ADR-046-Stable-Manager-Provisioned-Player-Join-Boundary.md)  
Execution route: [Framework IF-PLAN-ADR-046 v2](../../../../ImmersivePackages/com.immersive.framework/Documentation~/Architecture/Plans/IF-PLAN-ADR-046-STABLE-MANAGER-PROVISIONED-PLAYER-JOIN.v2.md)

## Purpose and boundary

Certify the public behavior of `PlayerSessionJoinTrigger` together with the Framework's scoped occurrence binding after I3 exists. This is a QAFramework scenario proposal for I4, not a new contract authority and not permission to implement I3 or the QA scenario. The internal `PlayerSessionJoinBindingLease` and `ValidateCurrent` remain package-owned and invisible to QA.

Package/NUnit remains responsible for deterministic mapping and internal mechanics. QAFramework validates the product through its real bootstrap, authored scenes, public calls, public outcomes, Session observations and Unity-owned physical resources. It does not claim a literal zero count of internal bridge invocations unless a separately retained package test proves that detail.

## Concrete infrastructure to reuse

The existing Slice 1 at `Assets/QA-IF-ADR-PLAYER-SESSION/` already authors a `GameApplication`, Manager-Provisioned Session/P1 Slot, Player Host prefab, explicit Persistent Content PlayerInputManager/endpoint registration, required technical Camera Output, and Route/Activity scenes. Its Configure command is idempotent and preserves unrelated `EditorBuildSettings.scenes`. Keep its GameApplication, scenario and expected 8-case contract unchanged.

Reuse its authoring conventions and share its validated Session/Slot/Host/Camera/Persistent assets only where a separate GameApplication can safely reference them without changing Slice 1's ownership or baseline. Add a separate QA-owned configuration/scenario area for IF-ADR-046, with its own Route/Activity assets and scenes. Do not add cases to Slice 1 or let Slice 1 setup delete/repair I4 assets.

Reuse two current causal patterns:

- `Assets/QA-NEW-003/Scripts/QaNew003RouteReplacementScenario.cs` for public Route request ordering, A→B→A, scene lifecycle evidence, frame budgets and restoration.
- `Assets/QA-NEW-002/Scripts/QaNew002ControlledActivityReadinessScenario.cs` for Activity replacement/re-entry occurrence observation, pending/terminal ordering and independent cleanup evidence.

These examples are patterns only; their runtime contracts and current assets must be rechecked against the exact resolved Framework before I4 implementation.

**Smallest proposed extension:** add an isolated `Assets/QA-IF-ADR-046/` owner with an idempotent `Editor/QaIfAdr046ScopedBindingSetup.cs`, a Persistent Content orchestrator in `Scripts/QaIfAdr046ScopedBindingScenario.cs`, and dedicated `Settings/` plus `Scenes/` for Route A/B, Activity A/B, and the Persistent Content composition. The dedicated GameApplication references the already validated Session/Slot/Host/Camera configuration where ownership permits. Keep the Manager endpoint and the orchestration object in the I4 Persistent scene so its lifetime spans transitions. A separate Manager-absent configuration omits the explicit registration and is run as an independent fresh boot. Keep Slice 1's generator and persisted scenes outside this extension. Do not create a general-purpose QA runner or shared runtime abstraction for these scenarios.

The separate setup must be idempotent, validate exact serialized cardinalities and scene ownership, preserve unrelated Build Profile/Editor Scene List entries, and fail on ambiguous duplicate fixtures. Every new Unity-recognized file gets a `.meta`. A future test asmdef that consumes `InputDevice`, `Keyboard`, `PlayerInput`, or another Input System type must reference `Unity.InputSystem` directly; asmdef references do not propagate transitively.

## Scenario shape

Use independent route-scope and activity-scope executions, each starting from a fresh valid GameApplication bootstrap and a known one-slot, Joining-closed, empty Session baseline. Separate runs avoid lifecycle contamination and keep each terminal causal.

The orchestrator belongs to the QA-authored Persistent Content scene. It uses only public Route/Activity request mechanisms, `PlayerSessionJoinTrigger` methods/outcomes, public Session observation and Unity scene/device APIs. It captures the old trigger reference and occurrence-specific public evidence while that scope is active, then reacquires the current trigger only from the exact authored scene for the new scope. Exact owner/cardinality is mandatory; no global search or Runtime Host lookup.

### Route binding run

1. Boot the QA-specific GameApplication through the installed Framework startup and wait for Route A plus its startup Activity and scoped trigger.
2. Capture Session revision/counts and the public trigger. Open Joining through the configured public Manager-provisioning authoring surface; invoke the Route-scoped trigger with a QA-owned synthetic Keyboard; require `Joined` and exactly one joined Slot. Independently verify Actor/gameplay state is not implied by that outcome. Leave through the public lifecycle, remove/unpair the device, close Joining, and confirm the empty baseline before the transition.
3. Request Route B, then Route A through public Route request triggers. Confirm the same Route A asset is active on return and the newly composed current trigger responds as a current binding (perform a controlled valid Join/Leave or equivalent public current-binding operation).
4. Invoke the captured A1 trigger reference after the A→B→A transition. Require typed `RejectedUnavailable`, unchanged Session revision/state, zero reservation/join and no new Host/PlayerInput. If Unity destruction makes calling the retained component unsupported or throws before a public outcome is produced, report this assertion `BLOCKED`; do not substitute the new A2 result or claim stale-lease proof.
5. Cleanup devices/Players, restore Joining closed and Route A baseline, then emit one terminal after resource checks.

### Activity binding run

1. Boot a separate valid GameApplication at Route A / Activity A and capture the Activity A1 trigger and Session baseline.
2. Prove A1's current binding through a public valid Join and Leave, with Actor/gameplay states checked independently.
3. Use supported Activity request triggers for A→B→A in the same Route. Confirm the Activity asset identity is the same on re-entry but the active scene consumer is a fresh current binding.
4. Invoke the captured Activity A1 reference. Require `RejectedUnavailable`, unchanged Session, no reservation/Join/Host/PlayerInput effect. A Unity-destroyed trigger that cannot safely produce the public result is `BLOCKED`, not PASS.
5. Change the owning Route occurrence and prove the previously captured Activity reference remains unavailable. Reacquire only the exact current Activity trigger and show current binding behavior.
6. Restore the declared Activity/Route baseline, release synthetic devices and physical Player resources, then emit the terminal.

### Negative and compatibility cases

Use explicit authored fixtures or supported lifecycle transitions; do not mutate Framework internals to invent states.

- **No Manager capability:** separate valid Session bootstrap without the registered provisioning endpoint; public Join returns `RejectedUnavailable` and Session remains unchanged. Do not disable or replace the Manager behind an active lease.
- **Scope mismatch:** a Route-discovered trigger authored for Activity scope (and the reciprocal case if needed) returns the specified configuration rejection; no Join effects occur.
- **Released/destroyed recipient:** exercise release through normal composition teardown. Invoke a retained public reference only if the trigger's ordinary managed call is safe after Unity destruction; otherwise report the invocation-specific proof as BLOCKED and retain package-owned deterministic coverage.
- **Legacy consumer:** keep the existing Experimental `PlayerSessionJoinCommandTrigger` in its current Slice 1 setup and independently rerun its regression; do not migrate serialized assets as part of this scenario.
- **Scene-Provided:** do not include it in Manager-Provisioned proof or interpret the new Join outcome as a change to Scene-Provided behavior.

There is no supported transition that makes an initialized Manager “not ready” or substitutes another Manager. Do not fabricate these states by disabling components, changing private fields or adding test-only Runtime controls. A bootstrap that cannot establish its required Manager is a BLOCKED precondition. Manager absence is tested as a separately authored valid configuration.

## Evidence and terminal contract

Before each action, record the Session revision, Joining state, Slot states/counts, currently active route/activity asset references, exact trigger/scene identity, synthetic device set, and any Host/PlayerInput evidence exposed by supported QA observation. After each action, record public outcome, Session revision/counts, Slot reservation/join state, current-scene trigger cardinality and physical host/device state.

The terminal must include scenario/test ID, case count, current scope/owner, captured versus reacquired component identity, public outcome, Session before/after, physical cleanup, first cause, unwind result and cleanup result. Use the QA certification recorder and a single stable terminal line. Preserve the first causal failure if cleanup also fails. `BaselineRestored` requires the actual final Session and physical resource checks; a zero Join count alone is insufficient.

For stale calls, public evidence can establish **no observable admission or side effect**: `RejectedUnavailable`, unchanged Session revision/state, zero Reserved/Joined delta and no new Host/PlayerInput/device ownership. It cannot, by itself, establish that an internal method was never called and then compensated. Do not phrase QA terminal fields or documentation as a literal invocation counter. If the exact no-delegation property remains a required Runtime invariant, prove that call boundary with package-owned deterministic tests, not a public test API.

## I4 exit criteria and limitations

I4 is accepted only when the user-run Unity execution records provenance for Framework/QA commits and dirty file fingerprints, Unity version, resolved package path/version, execution ID, raw log hash, each scenario terminal, and cleanup result. Retain FAIL/BLOCKED artifacts; never overwrite them with a later run. Run the public current/stale behavior on the same candidate bytes as the package tests.

I2/I3 certification requires all of the following:

1. Current scoped public trigger operates successfully in Route and Activity scopes.
2. Same-asset Route and Activity re-entry never makes the captured old binding usable; a distinct current trigger/binding is usable.
3. Stale/unavailable calls produce the exact public rejection with no Session or physical Player side effect.
4. Owner Route change invalidates the former Activity binding.
5. Manager-absent, mismatch, teardown and legacy consumer cases have their own evidence; impossible Manager transitions are not claimed as covered.
6. Cleanup returns Session and physical resources to the exact declared baseline.
7. FirstGame same-SHA compatibility remains a later I5 gate, not an inference from QA PASS.

If Unity does not permit a safe public call on a retained destroyed trigger, this test cannot certify that specific old-object invocation. Record the gap and keep I4 BLOCKED until a supported public lifecycle seam is defined by ADR review; do not add test-only backdoors. I4 blocks I5 when any mandatory case is BLOCKED or when public effect evidence is incomplete.
