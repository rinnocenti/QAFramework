# Vertical Slice Checklist

Before editing:

- Confirm the approved design and its current public endpoints.
- Confirm the active framework package source and relevant composition files.
- Confirm the minimum environment, owner identities, lifecycle acquisition moments, baseline, action, evidence, terminal condition, and cleanup ownership.
- Confirm that no prior execution residue is required.

During implementation:

- Keep setup/rebuild authoring separate from Scenario execution.
- Validate environment readiness before the action.
- Capture and verify the known baseline before mutation.
- Subscribe to causal evidence before invoking the supported action.
- Control an asynchronous release condition when an intermediate state is part of the proof.
- Detect missing, duplicate, out-of-order, wrong-source, or wrong-cardinality evidence.
- Preserve the first causal divergence; append cleanup information separately.
- Run cleanup in reverse ownership order on every terminal path and interruption.
- Verify restored baseline before publishing the verdict, or state that fresh boot is required.

Semantic gate:

- Before valid exercise: missing readiness, precondition, or observability is `BLOCKED`.
- After valid acceptance: missing terminal evidence, wrong typed result, or contract violation is `FAIL`.
- A framework defect can own a pre-action block without changing the Scenario verdict from `BLOCKED` to `FAIL`.
- Cleanup failure prevents `PASS`; it does not replace an earlier causal divergence.

Before handoff:

- Scan for forbidden private/internal access and hidden discovery.
- Confirm that only the approved slice was added.
- Confirm no generic execution architecture was introduced.
- Report created/changed/removed files, composition, evidence, ownership, cleanup, semantics, static checks, manual Unity steps, design divergences, open ADR decisions, and remaining risks.
