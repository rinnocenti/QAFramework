# QA Certification Review Contract

Verify and report:

1. ADR compliance and correct Package/NUnit, QAFramework, or FIRSTGAME placement.
2. Current public/supported surfaces and real composition used by the Scenario.
3. Known valid baseline verified before the action.
4. Causal order from perturbation/action through intermediate and terminal evidence.
5. Expected evidence declared before execution and missing-evidence detection.
6. Typed result and exact cardinality where relevant.
7. Correct `PASS`, `FAIL`, and `BLOCKED` semantics.
8. Preservation of the first causal divergence.
9. QA-owned versus framework-owned state and release in reverse ownership order.
10. Cleanup on success, failure, timeout, and interruption.
11. Restoration proof before verdict or explicit fresh-boot requirement.
12. Isolation and repeatability without residue or Full QA order.
13. Absence of parallel runtime, private/internal access, hidden lookup, and silent fallback.
14. ADR decisions that remain deliberately unresolved.

For each finding include severity, causal impact, file/line, owner, classification, missing evidence, and required action. When there are no findings, say so explicitly rather than inventing refactoring work.

Conclude with the certified contract, evidence considered, baseline/causality/cleanup/isolation assessments, open decisions, residual risks, and an unambiguous verdict limited to the reviewed Scenario or design.
