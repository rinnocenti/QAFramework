# Scenario Design Contract

Use this brief to decide whether a proposed runtime Scenario has a supported and independently reproducible composition.

1. **Verification level and reason:** Package/NUnit, QAFramework, or FIRSTGAME.
2. **Contract and category:** public contract; nominal, boundary, adversarial, or negative.
3. **Current evidence:** QA files/symbols, package resolution, public APIs, authoring surfaces, lifecycle, and observability proving the proposed path.
4. **Comparable QAs:** what can be reused and the conditions/side effects that limit reuse. Consult the [Unity QA authoring reference](../../qa-certification-implementation/references/unity-qa-authoring.md) for scene/asset composition.
5. **Minimum environment and dependency graph:** identify each dependency as preexisting or planned for generation.
6. **Asset plan:** for preexisting assets, verify exact active path, type, required references, and role. For generated assets, define type, role, inputs, supported creation surface, owner/lifetime, materialization order, and post-persistence acceptance checks. Generated assets need not already exist.
7. **Known baseline:** how the complete composition is checked before the action, without depending on prior Scenario residue.
8. **Supported action and acquisition timing:** identify the public action and when scope-bound endpoints are acquired.
9. **Expected evidence and terminal condition:** cardinality, typed outcomes, missing-evidence detection, and first causal divergence. For contracts involving object transfer, unload, replacement, or recreation, state which relevant object references, `EntityId`s, Scene path/handles, owners, and composition lifetimes must stay stable or may change. Omit identity domains the contract does not depend on.
10. **Verdicts:** `PASS` only when the contract is proved; `FAIL` only after valid exercise reveals a contract violation; `BLOCKED` when environment, precondition, materialization path, or observability prevents valid exercise.
11. **Ownership and cleanup:** release in reverse ownership order; restore the environment; state `baseline restored` or `fresh runtime boot required`.
12. **Gaps and unresolved decisions:** classify missing public observability/extension points and preserve decisions ADR-001 leaves open.

The causal shape is:

`valid environment → verified baseline → supported action → expected evidence → terminal condition → cleanup → restored baseline or fresh boot → verdict`

Do not mark `READY FOR IMPLEMENTATION` if a preexisting dependency is unverified, if a planned dependency lacks a supported materialization and validation path, or if required composition cannot be observed. Do not block solely because a correctly specified generated asset is not present before implementation.
