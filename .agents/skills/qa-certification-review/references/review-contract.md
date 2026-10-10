# QA Certification Review Contract

For Unity authoring details, use the single [Unity QA authoring reference](../../qa-certification-implementation/references/unity-qa-authoring.md).

Verify and report:

1. Correct Package/NUnit, QAFramework, or FIRSTGAME placement and ADR-001 compliance.
2. Current public/supported surfaces and real composition used.
3. Preexisting dependencies are actually resolvable; planned resources have a supported materialization path and post-persistence acceptance criteria.
4. The known valid baseline is verified before the Scenario action.
5. Setup order from source loads through scene operations and serialization does not reuse Unity wrappers across documented or demonstrated invalidation boundaries without reacquisition. For lifecycle identity claims, verify the retained Unity object reference, `EntityId`, Scene path/handle, and owner/lifetime independently as applicable.
6. Saved outputs are independently reloaded and checked for correct type, exact serialized links, cardinality, and semantic validity before setup success.
7. Causal order from supported action through intermediate and terminal evidence, including expected evidence declared before action and missing-evidence detection.
8. Correct `PASS`, `FAIL`, and `BLOCKED` semantics and preservation of the first causal divergence.
9. QA-owned versus Framework-owned state, cleanup after partial failure, reverse ownership release, and restoration before verdict.
10. Isolation without residual-state or Full QA order dependency.
11. No parallel runtime, private/internal access, hidden lookup, unowned environment mutation, or silent repair.
12. ADR-001 decisions that remain deliberately unresolved.

### Checkpoints and aggregate terminal evidence

Compare each checkpoint with aggregate counts and fields. Distinguish validated,
divergent, not reached, and `BLOCKED` checkpoints. `next`/`missing` fields must
not contradict completed evidence, and the terminal must preserve the first
causal divergence.

### External dependencies

When work depends on another repository, a local package, or a friend assembly,
inspect the manifest, effective package source, asmdefs, and authorized internal
access. Distinguish this workspace's resolution from reproducibility in another
checkout, and report external unversioned dependencies. Do not require a clean
checkout universally.

For each finding, include severity, causal impact, file/symbol evidence, owner, classification, missing evidence, and required action. Distinguish observed fact, documented risk, plausible hypothesis, and confirmed cause. Order findings by severity and causal impact. State explicitly which Unity compile/import/runtime/player validations were not executed.

Conclude with the reviewed contract, evidence, baseline/causality/cleanup/isolation assessments, open decisions, residual risks, and a verdict limited to the reviewed Scenario or design.
