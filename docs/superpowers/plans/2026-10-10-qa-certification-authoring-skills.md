# QA Certification Authoring Skills Implementation Plan

> **For agentic workers:** Execute one cut at a time. Do not start implementation until this plan is reviewed and approved. Do not delegate to subagents unless explicitly authorized.

**Goal:** Improve the existing Codex and Claude QA certification guidance so design, implementation, and review reuse verified Unity authoring patterns and validate persisted QA composition.

**Architecture:** Keep the three Codex skills and all current agent roles. Maintain one shared Unity authoring reference under the Codex Implementation skill; link to it from other skills and agents. Update only role-specific instructions and preserve ADR-001's unresolved architecture decisions.

**Tech Stack:** Markdown skill/reference documents, Codex TOML agent definitions, Claude Markdown agents, static repository checks.

**Spec:** `docs/superpowers/specs/2026-10-10-qa-certification-authoring-skills-design.md`

## Global Constraints

- Do not modify `Documentation~/ADR-001-QA-Framework-Certification-Architecture.md`.
- Do not modify IF-ADR-045 implementation, QA-NEW-004, Framework runtime, or public contracts.
- Create no new skill or agent; preserve current roles and invocation metadata.
- Do not run Unity, Play Mode, tests, builds, or batchmode.
- Do not commit or push.
- Preserve all preexisting working-tree changes and QA-owned/user-authored assets.
- Treat stale Unity-wrapper reuse across scene operations as a documented risk and pressure case, not as the proven cause of the historical IF-ADR-045 failure.
- Do not spawn subagents without explicit authorization.

## Review Focus

- Preexisting dependencies must be verified against the active repository/package source; planned generated assets need a supported materialization path and persisted postflight validation. Verify in Cut B.
- Unity wrappers must be reacquired after a documented or demonstrated invalidation boundary, not after every AssetDatabase call. Encode in Cut A and test in Cut E.
- Setup success must depend on independently reloaded persisted composition, including exact serialized references and required cardinality. Encode across Cuts A and B; verify in Cut E.
- Cleanup must be bounded by explicit QA ownership and safe after partial preparation. Encode in Cut A; verify in Cuts B and E.
- Review must find predictable ordering/composition defects before Play Mode while keeping Unity validation claims accurate. Encode in Cut B and agent prompts in Cuts C/D; verify in Cut E.

---

### Cut A: Shared Unity QA Authoring Reference

**Files:**

- Create: `.agents/skills/qa-certification-implementation/references/unity-qa-authoring.md`

**Interfaces:**

- Consumes: repository source and symbols listed in the approved specification.
- Produces: the single procedure reference linked by the Codex and Claude design, implementation, and review guidance.

- [ ] **Step 1: Write the source map** using `QA-NEW-004`, `QA-IF-ADR-042`, `QA-IF-ADR-043`, `QA-IF-ADR-044`, `QA-IF-ADR-045`, and `QA-NEW-005`. For each, record files/symbols, demonstrated behavior, preconditions, side effects, and limits. Do not use line numbers as durable identifiers.
- [ ] **Step 2: Document asset and template handling**: typed loads; main-asset/type/GUID checks where relevant; template copy; destination reload; `SerializedObject`; save and reload; validating the exact generated asset rather than only an in-memory object.
- [ ] **Step 3: Document scene and reference lifetime**: scene setup capture/restoration; dirty/untitled scene protection; `NewScene(Single)` invalidation warning from IF-ADR-044; reacquisition after evidenced invalidation boundaries; no mechanical reload after every AssetDatabase operation.
- [ ] **Step 4: Document four setup gates**: preflight, construction, postflight, cleanup. Preflight validates dependency types, references, and composition roles; postflight reloads persisted assets and checks the baseline; cleanup enumerates owned paths and partial-failure behavior.
- [ ] **Step 5: Add an evidence classification section** distinguishing observed fact, repository-documented risk, plausible hypothesis, and confirmed cause. Include IF-ADR-045 only as a plausible stale-reference pressure case; explicitly state that its exact historical null cause was inconclusive.

**Verification:** Inspect every named symbol in the current source; confirm links/file names exist; search the reference for unsupported universal claims and stale-root-cause assertions.

**Acceptance:** One concise reference gives future authors actionable patterns and limits without duplicating QA implementations or presenting hypotheses as facts.

### Cut B: Codex Skills and Existing Skill References

**Files:**

- Modify: `.agents/skills/qa-certification-design/SKILL.md`
- Modify: `.agents/skills/qa-certification-design/references/scenario-contract.md`
- Modify: `.agents/skills/qa-certification-implementation/SKILL.md`
- Modify: `.agents/skills/qa-certification-implementation/references/vertical-slice-checklist.md`
- Modify: `.agents/skills/qa-certification-review/SKILL.md`
- Modify: `.agents/skills/qa-certification-review/references/review-contract.md`

**Interfaces:**

- Consumes: Cut A's shared reference and ADR-001.
- Produces: compact role-specific workflows with no duplicated authoring manual.

- [ ] **Step 1: Add Design's reuse gate** for contract/verification level, comparable QA evidence, minimum composition, dependency graph, persisted versus planned assets, ownership/lifetime, public surfaces, baseline, cleanup, and unresolved gaps. State that planned resources need not exist physically if type, role, dependency graph, supported creation surface, owner/lifetime, materialization sequence, and persisted validation criteria are established. `READY FOR IMPLEMENTATION` requires a supported, demonstrable path to materialize and observe the composition.
- [ ] **Step 2: Add Implementation's four gates**. Preflight validates actual dependencies and composition roles before generation; Construction reacquires references at evidenced invalidation boundaries; Postflight saves, reloads, and validates persisted composition; Cleanup handles only owned state and partial preparation. Preserve existing public-surface and PASS/FAIL/BLOCKED rules.
- [ ] **Step 3: Add Review's pre-Play Mode order audit** from sources through cleanup, explicitly checking whether references cross documented/observed invalidation operations, whether serialized outputs resolve, and whether postflight uses independently reloaded assets.
- [ ] **Step 4: Link each entry point/reference to the one shared authoring file** and remove duplicated procedural detail only when the shared link preserves the actual gate. Keep each skill's invocation purpose distinct.

**Verification:** Check each `SKILL.md` frontmatter/name and all relative links. Compare role responsibilities against ADR-001; verify Design does not reject planned assets solely for not existing and does not mark unsupported composition ready.

**Acceptance:** Design, Implementation, and Review have distinct operational gates, use the shared reference, and retain ADR-001 semantics.

### Cut C: Codex Agent Instructions

**Files:**

- Modify: `.codex/agents/qa-certification-architect.toml`
- Modify: `.codex/agents/qa-certification-implementer.toml`
- Modify: `.codex/agents/qa-certification-reviewer.toml`

**Interfaces:**

- Consumes: Cut A's reference and Cut B's skill gates.
- Produces: explicit role prompts consistent with the corresponding skills.

- [ ] **Step 1: Update the architect prompt** to require the Design reuse gate and to distinguish existing dependencies from supported planned generation.
- [ ] **Step 2: Update the implementer prompt** to require preflight/build/postflight/cleanup and persisted-output checks before success.
- [ ] **Step 3: Update the reviewer prompt** to inspect source-to-cleanup order before first Play Mode and classify facts, risks, and hypotheses correctly.
- [ ] **Step 4: Preserve agent names, descriptions, read-only modes, and standalone TOML fields.**

**Verification:** Parse/inspect TOML structure; compare each prompt with its matching skill; confirm no role gains write or delegation authority beyond the current definition.

**Acceptance:** Each Codex agent routes to its existing skill and reinforces only its own gate.

### Cut D: Claude Skill and Agent Integration

**Files:**

- Modify: `.claude/skills/qa-scenario-design/SKILL.md`
- Modify: `.claude/skills/qa-greenfield-slice/SKILL.md`
- Modify: `.claude/agents/qa-scenario-writer.md`
- Modify: `.claude/agents/qa-adr-auditor.md`

**Interfaces:**

- Consumes: Cut A's shared reference and the applicable Design/Review requirements from Cut B.
- Produces: Claude guidance aligned in evidence and boundaries while retaining its current design, writer, sequencing, and auditor responsibilities.

- [ ] **Step 1: Route Claude design and greenfield guidance** to the shared authoring reference only where asset/scene authoring is relevant. Preserve ADR-001 sequencing and do not make planned generated assets a design blocker by themselves.
- [ ] **Step 2: Update the scenario writer** to follow preflight, ordered construction, persisted postflight, and owned cleanup before reporting setup success.
- [ ] **Step 3: Update the ADR auditor** to check operation ordering, stale-wrapper risk, serialized baseline, and cleanup before Play Mode; distinguish QA defects, Framework defects, environment blocks, and observability gaps.
- [ ] **Step 4: Keep Claude responsibilities and metadata intact.** Do not create implementation/review agents or additional skills.

**Verification:** Resolve the cross-runtime relative links to the exact shared file; inspect Claude frontmatter/tool declarations for unintended role or permission changes.

**Acceptance:** Claude agents and skills consume the same evidence without copying the reference or acquiring duplicate roles.

### Cut E: Static Verification and Walkthroughs

**Files:**

- No additional files expected. Fix only files from Cuts A–D if a verification defect is found.

**Interfaces:**

- Consumes: completed reference, skills, and agent instructions from Cuts A–D.
- Produces: static verification evidence and a concise completion report. No Unity certification claim.

- [ ] **Step 1: Validate structure and metadata** for all three Codex skills, Claude skills, Codex TOML agents, and Claude agent Markdown. Confirm existing invocation settings remain unchanged.
- [ ] **Step 2: Resolve every relative reference** from each Codex/Claude skill to the same shared authoring file; detect dangling references and accidental duplicate authoring manuals.
- [ ] **Step 3: Run the new-QA design walkthrough.** Give Design a contract with a mix of existing dependencies and planned generated assets. Confirm it verifies existing dependencies and accepts planned assets only with a supported creation path, ownership, sequence, and persisted acceptance criteria.
- [ ] **Step 4: Run the static ordering-risk walkthrough.** A valid typed template is loaded; a `NewScene(Single)` operation documented as potentially invalidating wrappers occurs; the old wrapper is reused; the agent must identify the risk and require reacquisition at the appropriate boundary, then independently reload and validate the persisted destination. Label this as a documentation walkthrough, not an agent behavior test and not proof of IF-ADR-045's historical cause.
- [ ] **Step 5: Check ADR-001 consistency, four-gate coverage, PASS/FAIL/BLOCKED semantics, ownership and scope constraints; run `git diff --check`.**

**Verification:** Static checks only. Do not invoke Unity, tests, Play Mode, builds, or subagents.

**Acceptance:** Every path and metadata reference resolves, both walkthroughs satisfy their expected decisions, no instruction contradicts ADR-001, and `git diff --check` passes. Report Unity compilation/import/runtime validation as not performed.

## Execution Boundary

This plan contains Cuts A–E only. Do not begin them until the user reviews and approves this plan. Keep the approved IF-ADR-045 setup changes untouched; later use may evaluate the guidance but is outside these cuts.
