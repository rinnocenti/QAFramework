# Vertical Slice Checklist

Read the [Unity QA authoring reference](unity-qa-authoring.md) for evidence-backed asset, scene, persistence, and cleanup patterns.

## Preflight

- Confirm approved design, active package source, current public endpoints, and exact verification level.
- Verify every preexisting dependency by type, expected references, and its role in the intended composition; file existence alone is insufficient.
- For planned outputs, record type, inputs, supported creation method, owner/lifetime, dependency order, and postflight checks.
- Check dirty/untitled scenes, destination collisions, partial prior generation, and any environment setting the setup may change.

## Construction

- Keep setup/rebuild authoring separate from Scenario execution.
- Materialize in dependency order; preserve stable paths across phases.
- Reacquire Unity references after an operation documented or demonstrated to invalidate wrappers. Avoid unconditional reload after every AssetDatabase call.
- Bind exact assets/components, verify expected cardinality and serialized references, and save scenes/prefabs/assets at dependency boundaries.

## Postflight

- Independently reload outputs by canonical paths after persistence.
- Verify exact serialized references/values, types, cardinality, scene/prefab component bindings, and contract-specific semantics.
- Validate the complete baseline before setup reports success or the Scenario action begins.

## Cleanup and terminal evidence

- Release owned state in reverse ownership order; restore captured environment mutations through their owner.
- Run cleanup after success, failure, and partial preparation. Delete only enumerated QA-owned assets; preserve unknown files and report residuals.
- Preserve the first causal divergence and report unwind/cleanup separately.
- `BLOCKED` applies when the valid exercise cannot begin; `FAIL` applies after the supported action is validly accepted and the contract diverges. Cleanup failure prevents `PASS`.
- Verify restored baseline or state the fresh-boot requirement before publishing the runtime verdict.

## Before handoff

- Scan for private/internal access, hidden lookup, silent fallback, unowned mutation, stale references across invalidation boundaries, and success before persisted postflight.
- Report files, composition, evidence, ownership, cleanup, static checks, manual Unity steps, and all unexecuted validation.
