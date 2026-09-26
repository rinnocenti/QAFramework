# Scenario Design Contract

Produce an evidence-backed design brief with these fields:

1. Verification level and justification: Package/NUnit, QAFramework, or FIRSTGAME.
2. Public contract under proof and Scenario category: nominal, boundary, adversarial, or negative.
3. Current files and public symbols that prove the available composition and lifecycle.
4. Minimum real Test Environment and owner of every composed part.
5. Known valid baseline and how it is verified before the action.
6. Supported public action and the correct acquisition/execution moment.
7. Expected evidence declared before execution, including cardinality and typed outcomes where relevant.
8. Terminal condition and missing-evidence detection.
9. PASS, FAIL, and BLOCKED semantics. BLOCKED means the contract was not correctly exercised because environment, precondition, or observability was unavailable.
10. First causal divergence preservation.
11. Cleanup on success, failure, and interruption, in reverse ownership order.
12. Proof of restored baseline or an explicit fresh-boot requirement.
13. Public observability or supported-extension gaps and their correct owner.
14. ADR decisions that remain deliberately unresolved.

The causal shape is:

`valid environment -> known baseline -> supported action -> expected evidence -> terminal condition -> cleanup -> baseline restored or fresh boot -> verdict`

Reject a design that relies on previous Scenario residue, Full QA order, environment repair during execution, or evidence discovered only after the action.
