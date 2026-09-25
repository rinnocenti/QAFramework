# QA Certification Skills and Agents Design

## Status

Proposed for implementation after user review.

## Purpose

Create repository-owned Codex skills and agent roles that guide future QA work according to `Documentation~/ADR-001-QA-Framework-Certification-Architecture.md`.

The system must help future contributors classify a proof at the correct verification level, design an independently reproducible runtime Scenario, implement a complete vertical slice, and review the resulting certification without recreating the removed legacy QA architecture.

## Source of truth

`ADR-001-QA-Framework-Certification-Architecture.md` remains the canonical architectural authority.

The new skills translate the ADR into repeatable workflows. They must link to the ADR and must not copy it wholesale, reinterpret deliberately unresolved decisions as settled, or become a second architecture document.

Legacy QA skills, agent instructions, smoke conventions, runners, panels, phase machines, fixtures, folder layouts, class names, and the historical Smoke/Regression split are not compatibility requirements. New repository-owned guidance supersedes older QA-specific guidance. Obsolete QA-specific skills and agent roles may be removed after the new set passes validation.

## Intended users

- An architect deciding where a contract should be proved and shaping a QA Scenario.
- An implementer creating one complete QAFramework vertical slice.
- A reviewer auditing a proposed or implemented Scenario against the ADR.
- A coordinating Codex task that delegates one bounded responsibility to a specialist agent.

## Repository layout

```text
.agents/
  skills/
    qa-certification-design/
      SKILL.md
      agents/openai.yaml
      references/scenario-contract.md
    qa-certification-implementation/
      SKILL.md
      agents/openai.yaml
      references/vertical-slice-checklist.md
    qa-certification-review/
      SKILL.md
      agents/openai.yaml
      references/review-contract.md
.codex/
  config.toml
  agents/
    qa-certification-architect.toml
    qa-certification-implementer.toml
    qa-certification-reviewer.toml
docs/
  superpowers/
    specs/
    plans/
```

Repository-local skills use the portable `.agents/skills/` convention. `.codex/config.toml` declares the three custom Codex roles and resolves each role configuration relative to that file.

## Skill boundaries

### `qa-certification-design`

Use when deciding whether a contract belongs in Package/NUnit, QAFramework, or FIRSTGAME, or when designing a new QAFramework Scenario.

It must produce a design brief containing:

- verification level and justification;
- public contract under proof;
- Scenario category: nominal, boundary, adversarial, or negative;
- minimum real Test Environment;
- known valid baseline;
- supported action;
- expected evidence known before execution;
- terminal condition;
- cleanup owner and reverse ownership order;
- baseline restoration or explicit fresh-boot requirement;
- observability or extension-point gaps;
- unresolved implementation decisions that remain unresolved.

It must stop before implementation when the contract cannot be exercised or observed through supported public surfaces. It classifies the gap instead of inventing reflection, global discovery, internal-host access, a service-locator bypass, or a test-only framework backdoor.

### `qa-certification-implementation`

Use after an approved Scenario design exists and the task explicitly requests implementation.

It must implement one complete vertical slice from a known valid baseline through terminal evidence and cleanup. It owns only QAFramework files unless evidence proves a separate framework defect and the user explicitly expands the task.

It must require:

- real framework lifecycle and supported authoring;
- public framework APIs;
- minimum composition for the contract;
- setup/rebuild separated from Scenario execution;
- predeclared expected evidence;
- explicit PASS, FAIL, and BLOCKED semantics;
- preservation of the first causal divergence;
- cleanup on success and failure;
- restoration evidence before verdict publication;
- controlled release of an asynchronous condition when an intermediate state is part of the proof;
- static validation plus a manual Unity validation checklist.

It must not generalize common Runner abstractions from a single slice. Shared execution infrastructure is extracted only from evidence supplied by the first two representative slices.

### `qa-certification-review`

Use for architecture reviews, implementation reviews, failed certification diagnosis, and readiness assessment before expanding coverage.

It must report:

- verdict on ADR compliance;
- findings ordered by severity and causal impact;
- evidence with file and line references;
- correct owner for each finding;
- classification as framework contract violation, QA defect, environment/precondition block, or observability gap;
- missing evidence and the first causal divergence;
- cleanup and restoration assessment;
- decisions that are still deliberately unresolved;
- required manual Unity checks without claiming PASS.

The reviewer is read-only unless the user separately requests fixes.

## Agent roles

### `qa-certification-architect`

Selects and applies `qa-certification-design`. It may inspect framework public contracts and QA composition but does not implement runtime code. Its output is an actionable Scenario design brief and an explicit list of decisions that remain open.

### `qa-certification-implementer`

Selects and applies `qa-certification-implementation`. It receives an approved Scenario design and implements the complete QAFramework cut. It does not change framework packages, FIRSTGAME, or frozen technical packages unless the task explicitly authorizes that separate owner.

### `qa-certification-reviewer`

Selects and applies `qa-certification-review`. It performs an independent, evidence-based, read-only audit. It does not silently repair defects or reinterpret missing evidence as success.

## Coordination model

The roles are specialists, not a mandatory pipeline. A root task delegates only a concrete bounded responsibility:

1. The architect is used when Scenario placement or design is undecided.
2. The implementer is used only after a design is approved.
3. The reviewer is used after a design or implementation exists, or when a certification failure needs classification.

Short sequential work remains with the root agent. Parallel agents are reserved for independent reviews or investigations that do not edit shared files.

## Behavioral validation

Each skill is created and validated separately.

Before writing a skill, a baseline scenario must demonstrate the failure that the skill is intended to prevent. Representative pressures include:

- placing deterministic contract logic in QAFramework because the user called it QA;
- recreating a legacy runner before two vertical slices provide evidence;
- treating residual runtime state as a fixture;
- using private reflection because public observability is missing;
- emitting PASS before cleanup or restoration completes;
- collapsing BLOCKED into FAIL;
- changing the framework to make QA convenient;
- coupling a Scenario to Full QA execution order.

After creation, the same scenarios are rerun with the skill. Success requires correct level classification, a complete causal Scenario contract, preservation of unresolved decisions, and no unauthorized implementation.

Structural validation must check:

- valid `SKILL.md` frontmatter and directory naming;
- discriminating descriptions beginning with `Use when...`;
- discoverable references;
- valid `agents/openai.yaml` metadata;
- valid Codex TOML and relative role paths;
- absence of unfinished placeholders;
- absence of references to removed legacy QA architecture as a required implementation.

No Unity build, Play Mode, batchmode, smoke, or runtime certification is executed by this work.

## Replacement policy

The repository-owned skills and roles become the only active QA-specific guidance for this project after validation.

An older QA-specific personal skill may be removed to prevent conflicting automatic invocation. General Unity, package, debugging, planning, and verification skills remain intact because they are not older versions of this QA architecture.

No legacy QA implementation under untracked or user-owned files is deleted merely by inference. Any source deletion must identify exact paths and prove that they are obsolete QA artifacts rather than current user work.

## Non-goals

- Implementing the QA Runner.
- Choosing concrete Scenario, Environment, result, discovery, menu, persistence, timeout, boot-grouping, folder, or asmdef designs.
- Creating the first certification Scenario.
- Porting historical smoke infrastructure.
- Modifying Immersive Framework packages or FIRSTGAME.
- Running Unity validation.

## Acceptance criteria

- Three focused repository-local skills exist and link to the ADR.
- Three Codex agent roles exist and are declared by project configuration.
- Each role has one narrow responsibility and routes to the matching skill.
- The skills preserve Package/NUnit, QAFramework, and FIRSTGAME boundaries.
- The skills require known baselines, supported actions, explicit evidence, cleanup ownership, and restoration before verdict.
- The skills preserve PASS, FAIL, and BLOCKED semantics and the first causal divergence.
- The skills forbid parallel runtime architecture, accidental residue, silent environment repair, and test-only framework backdoors.
- Deliberately unresolved ADR decisions remain unresolved.
- Obsolete QA-specific guidance is removed only after the replacement validates.
- Static validation passes, and manual future-use checks are documented.

