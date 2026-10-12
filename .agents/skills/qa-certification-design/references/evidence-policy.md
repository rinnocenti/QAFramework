# QA Certification Evidence Review Checklist

This checklist supports certification design, implementation, and independent review. [IF-GOV-001 §8](https://github.com/ImmersiveGames/com.immersive.framework/blob/master/Documentation~/Architecture/Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md#8-evidence-proportionality-and-certification) is the single normative source for general evidence proportionality; QA-specific procedure is defined by ADR-001 §12.1. This file operationalizes those requirements. It does not change ADR-001's execution model or waive a contract-specific certification gate.

## Reconcile before requesting new proof

Before asking for another execution or proposing a fixture, state the property
and decision scope, locate accessible existing tests/results/observations and
reviews, map what each source actually demonstrates, consider complementary
evidence, and list uncovered properties and residual risks. Propose new proof
only for a material technical gap that the applicable gate requires or that the
responsible authority cannot accept. A lack of one fixture that covers an
entire property is not by itself a gap. Name the risk and smallest viable
intervention at its owning layer before recommending infrastructure.

Evidence access is task-scoped. When relevant artifacts live in another
conversation or session, identify what must be transferred, confirm the task
can read the supplied originals, and distinguish originals from transcripts or
summaries. If they are inaccessible, report the access limitation; do not
conclude that evidence is absent or was never run.

## Keep three questions separate

Record these separately:

1. **Evidence result:** what each test, integration run, static inspection, or other source actually returned, including scope, provenance, and limitations.
2. **Property coverage:** which gate requirements those sources support, and which remain unobserved, untested, or otherwise uncovered.
3. **Gate decision:** whether the defined gate is accepted, conditionally accepted, or blocked under its own requirements.

Never rewrite an individual `PASS`, `FAIL`, or `BLOCKED`, promote a scoped result to an aggregate result, or describe static inspection as an executed test. A composite decision is an acceptance judgment over preserved evidence; it is not a new test result.

## Proportionate evidence selection

Start by inventorying existing evidence and the current public, consumer-observable contract. Map every requirement of the gate to its evidence and identify the evidence type, exact scope, provenance, and limitation. Prefer existing deterministic tests, real public integration evidence, and focused source review when together they adequately address the relevant risks.

Evidence types are complementary, not interchangeable, and no format is
universally required or sufficient:

- Deterministic/unit tests support the behavior and boundaries they execute, often at package level; they do not establish real Unity lifecycle or consumer integration by themselves.
- Executed public integration supports only the observed contract, configuration, runtime, candidate bytes when captured, and conditions recorded for that run.
- Static source review supports code-path, ownership, invariant, and plausible defect analysis; it does not prove execution, compilation, import, runtime behavior, or integration.
- User-reported or otherwise incomplete-provenance results retain that provenance limitation and must not be retrospectively attached to a commit or candidate.

Accept a gate using **composite evidence** only when all of the following hold:

1. The gate's governance does not require a particular proof type, execution, candidate identity, or other unmet condition.
2. Every gate requirement is accounted for and marked sufficiently supported, partially supported, or uncovered, with limitations explicit; no material requirement is silently treated as covered. Any partial or uncovered requirement must be shown not to invalidate the exact bounded claim being accepted, or the disposition must be conditional/blocked.
3. The combination addresses the risk of the claim being accepted at the gate's stated scope. Correlated evidence is not counted as independent confirmation.
4. The decision states why remaining gaps do not invalidate this scoped acceptance and records residual risk and follow-up conditions, if any.
5. Individual outcomes and provenance remain unchanged and visible.

Do not turn “some evidence exists” into coverage. An untested property may be adequately constrained by other evidence for a bounded acceptance only where the governing gate permits that judgment; it remains untested.

## Decision vocabulary

- **PASS (direct):** required evidence directly exercises and satisfies the scoped requirement under its specified conditions, with required setup, cleanup, and restoration complete. Report provenance captured; completeness is required only to the extent an applicable gate explicitly requires it.
- **ACCEPTED (composite):** a gate-level decision, not an execution verdict. The evidence map justifies acceptance under the conditions above while preserving individual results, uncovered properties, provenance limits, and residual risks. Do not relabel `BLOCKED` evidence as `PASS`.
- **CONDITIONALLY ACCEPTED:** a bounded gate decision where current evidence supports only a narrower claim or acceptance depends on explicit follow-up/conditions. Name the accepted scope, excluded or unproven claims, owner/trigger for follow-up where known, and conditions that reopen the gate. This is not overall certification closure or Stable promotion unless the governing authority explicitly says so.
- **BLOCKED:** the scoped requirement cannot be validly decided because a required precondition, supported exercise/observation path, or explicitly mandated evidence is unavailable. Missing provenance alone is a limitation, not a functional failure or blocker, unless an applicable gate explicitly makes that provenance a decision prerequisite. Preserve partial evidence and name the concrete blocker. `FAIL` remains reserved for a validly exercised contract that diverged.

If the local reporting system only supports `PASS`/`FAIL`/`BLOCKED`, preserve those fields and record composite or conditional acceptance separately as the gate disposition. Do not invent a runtime verdict or alter serialized result semantics to fit this policy.

## Governance and distinct decisions

Read applicable IF-GOV, ADR, amendment, release, and certification gates before disposition. Contract-specific mandatory proof, candidate identity, replay, consumer import/Play Mode, regression, cleanup, or compatibility requirements remain mandatory. Provenance fields are mandatory only when a governing gate states them or their risk-based purpose is explicit. Where wording conflicts or scope is ambiguous, preserve the accepted gate, identify the exact clause, and use its amendment process; do not silently waive it or repeat execution only to reconstruct historical metadata.

Keep distinct:

- certification of one increment/Scenario;
- acceptance of an integrated change or consumer workflow;
- compatibility regression after migration/removal;
- API maturity or Stable promotion;
- package release.

Acceptance at one level does not imply another. Stable promotion and release follow their own authorization and evidence gates. Evidence from an earlier run is not associated retrospectively with a later commit or candidate.

## Infrastructure discipline and reopening

Do not add fixtures, seams, mocks, internal accessors, observability APIs, or shared QA infrastructure merely to make coverage look complete. Before recommending added infrastructure, state objectively: (a) the risk, (b) the exact property not covered, (c) the evidence absent and why current evidence is insufficient under governance, and (d) the smallest viable intervention at the correct owner. A mock/fake is suitable only for the contract it can actually model; it cannot stand in for real integration.

Reopen or expand coverage when a concrete defect is found, a relevant public contract or implementation boundary changes materially, a regression is demonstrated, residual risk becomes unacceptable, or an explicit normative gate requires it. Do not reopen a closed increment merely to obtain aesthetically complete coverage when none of these triggers applies.

Use stricter direct proof when risk is high, an invariant is critical, a regression has been demonstrated, evidence is materially ambiguous, or governance explicitly mandates certification. Proportionality does not lower the standard for these cases.

## Required record for a composite or conditional decision

Record the gate and claim scope; requirement-to-evidence map; each source's type, result, provenance, and limitation; uncovered properties; applicable governance clauses; acceptance rationale; residual risks; explicit conditions/reopen triggers; and the separate gate disposition. Do not fabricate results, provenance, or commit association.
