# FXX-CLOSEOUT - MAT-3 Materialization / Release Ownership Closeout

Status: Closed / MAT-3
Date: 2026-06-30

## 1. Decision

MAT-3 closes the RuntimeContent / ContentAnchor materialization and release ownership track.

MAT-1 and MAT-2 are accepted as passed within this track:

- MAT-1 consolidated the physical release + logical release sequencing into `ContentAnchorReleaseExecution`.
- MAT-2 consolidated ContentAnchor binding cleanup ownership into `ContentAnchorBindingCleanup`.

No further safe mechanical duplication was found on the remaining `materialize -> bind -> release -> cleanup` axis that could be extracted without broadening the scope into a larger service or host-level integration.

## 2. Remaining duplication sweep

Reviewed areas:

- materialization
- lifecycle register
- binding
- physical release
- logical runtime release
- binding cleanup

Decision by area:

- materialization: already consolidated enough for this track by MAT-1.
- lifecycle register: no safe shared seam found without broadening `RuntimeContentRuntime`.
- binding: logical binding ownership remains in `RuntimeContentAnchorBinding`; no new seam needed.
- physical release: already consolidated enough for this track by MAT-1.
- logical runtime release: already consolidated enough for this track by MAT-1.
- binding cleanup: already consolidated enough for this track by MAT-2.

## 3. Boundary preserved

- Route and Activity remain the semantic owners of lifecycle decisions.
- ContentAnchor remains the mechanical owner of binding/release sequencing.
- No public API changed.
- No enum changed.
- No asmdef changed.
- No package.json changed.
- No scene, prefab or asset changed.
- No new lifecycle was introduced.
- No broad service was introduced.

## 4. What was not changed

- `RouteLifecycleRuntime` and `ActivityFlowRuntime` were not expanded in this cut.
- `FrameworkRuntimeHost` was not widened.
- Diagnostics texts and counts were not normalized.
- Smokes were not adjusted to accept drift.

## 5. Validation performed

Static review only.

I rechecked the materialize/bind/release/cleanup axis and confirmed the remaining duplication is either already absorbed by MAT-1/MAT-2 or too coupled to host/broad-service boundaries to extract safely in this cut.

No Unity compile, import or smoke was run in this turn.

## 6. Manual validation checklist

1. Unity compile/import
2. Standard Smoke
3. Content Anchor Diagnostics Smoke
4. Activity Content Anchor Diagnostics Smoke
5. Composite Lifecycle Release Smoke
6. Route Release Smoke
7. Activity Content Execution Participant Source Smoke

## 7. Files changed in MAT-3

- `Assets/_Documentation/Closeouts/FXX-CLOSEOUT-MAT-3-Materialization-Release-Ownership-Closeout.md`
- `Assets/_Documentation/Plans/FXX-PLAN-RuntimeContent-ContentAnchor-Materialization-Orchestration.md`
- `Assets/_Documentation/Plans/FXX-PLAN-Architecture-Consolidation-Roadmap.md`

## 8. Risk

The only remaining seams are intentionally left at the current ownership boundaries.
If a future cut wants to merge them, it should come with a separate ADR and a broader boundary decision.
