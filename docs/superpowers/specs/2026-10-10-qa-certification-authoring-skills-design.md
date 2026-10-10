# QA Certification Authoring Skills — Design

## Goal

Improve the existing QA certification skills and agents so new QA scenarios reuse verified Unity setup, serialization, persistence, validation, and cleanup patterns from this repository. Use IF-ADR-045 as a later effectiveness case without changing its implementation in this work.

## Scope and constraints

- Keep the three Codex skills: `qa-certification-design`, `qa-certification-implementation`, and `qa-certification-review`.
- Add one shared reference at `.agents/skills/qa-certification-implementation/references/unity-qa-authoring.md`; other skills link to that file and do not copy its procedures.
- Align existing Codex and Claude skills/agents while preserving their different discovery and role models. Create no skill or agent.
- Preserve `Documentation~/ADR-001-QA-Framework-Certification-Architecture.md` and its undecided implementation choices.
- Do not change Framework runtime, public contracts, QA-NEW-004, or IF-ADR-045 implementation. Do not run Unity, Play Mode, tests, builds, or commit/push.

## Repository evidence to encode

| Source | Demonstrated pattern | Limit/lesson |
|---|---|---|
| `Assets/QA-NEW-004/Settings`, `Assets/QA-NEW-004/Scenes`, `Documentation~/QA-NEW-004-Certification.md` | Authored, persisted environment with an independently executed runtime scenario. | Use as a persisted-baseline reference, not as proof of a generator workflow. |
| `Assets/QA-IF-ADR-042/Editor/QaIfAdr042Setup.cs` — `Configure`, `CreateAsset`, `CreatePersistentScene`, `SetPersistentScene`, `ValidateGeneratedBaseline` | Typed asset loading; staged save/reload; scene/prefab persistence; a final baseline check. | The setup updates existing QA assets and modifies Build Settings; those effects are not general requirements. |
| `Assets/QA-IF-ADR-044/Editor/QaIfAdr044Setup.cs` — `Configure`, `CreateApplication`, `CreatePersistentScene`, `BindPersistentScene`, `ValidateGeneratedBaseline` | Copy a validated GameApplication template, reload assets after scene generation, and validate the saved baseline. `CreatePersistentScene` documents that `NewScene(Single)` can invalidate wrappers loaded before the scene switch. | This is a documented Unity-reference invalidation risk and a plausible explanation for D7, not proof of D7's root cause. Reacquire after operations documented or demonstrated to invalidate wrappers; do not reload after every AssetDatabase call mechanically. |
| `Assets/QA-IF-ADR-043/Editor/QaIfAdr043Setup.cs` — `Configure`, `ValidateGeneratedBaseline` | Persist, refresh, and verify a generated QA baseline. | Its setup mutates existing build configuration; document the scope before reuse. |
| `Assets/QA-NEW-005/Editor/QaNew005Setup.cs` | Generated audio/camera/scene setup with scoped scene restoration and persistence steps. | Treat each operation's actual effects as evidence; `Refresh` is not itself a universal reference-invalidation rule. |
| `Assets/QA-IF-ADR-045/Editor/QaIfAdr045Setup.cs` — `Prepare`, `CreatePersistentScene`, `CreateApplication`, `LoadRequiredSourceAsset` | Strong source preflight, dirty/untitled scene guard, QA-owned generated paths, scoped cleanup, typed asset creation and serialization. | It carried Unity object references across operations that IF-ADR-044 documents as potentially invalidating wrappers. Unity confirmed the template itself was loadable, but the exact cause of the later null reference remained inconclusive. Treat stale-wrapper reuse as a plausible risk, not a proven historical root cause; do not edit D7 here. |

The reference will distinguish observed behavior from recommended practice. Each pattern will name the relevant file and symbol, explain its preconditions and side effects, and identify practices that must not be copied outside those conditions. It will not use line numbers as durable identifiers.

## Shared reference ownership and discovery

The shared reference has one owner under the Codex Implementation skill. Existing Codex skills already link outside their own skill directory to `Documentation~`. Claude skills do likewise. Relative-path resolution was checked from both Claude skills and the sibling Codex skills: each path normalizes to the same repository file. The implementation skill links locally; Design and Review use sibling-relative links; Claude skills use repository-relative links. Existing agent files can read repository files and already name skills explicitly.

Preserve `allow_implicit_invocation` and existing agent identities. Update only prompts/instructions needed to consume the shared reference and enforce their role-specific gates. The Codex agent TOMLs remain the architect, implementer, and reviewer. Claude keeps its current scenario writer, ADR auditor, design skill, and greenfield sequencing skill; no artificial one-to-one agent mirror is introduced.

## Skill behavior

### Design

Before `READY FOR IMPLEMENTATION`, require an evidence-backed brief containing: contract and verification level; comparable current QAs; reusable patterns with file/symbol evidence; minimum Unity composition and dependency graph; preexisting and planned assets; ownership/lifetime; supported public surfaces; baseline; evidence and cleanup; and unresolved gaps/decisions. Verify preexisting dependencies against repository reality. Planned generated resources need not exist yet, but their type, purpose, dependencies, supported creation surface, owner/lifetime, materialization order, and post-persistence validation criteria must be defined. Remain `BLOCKED` when no supported, demonstrable path exists to materialize or observe required composition.

### Implementation

Use four gates, without a generic setup framework:

1. **Preflight:** confirm active package/source, type and assembly, exact asset paths/GUIDs where relevant, typed loads, required object references and their role in the intended composition, camera/scene prerequisites, path collisions/residuals, and ownership/cleanup boundaries before generating anything.
2. **Construction:** follow an explicit dependency order. Reacquire Unity references after operations shown by repository evidence to invalidate wrappers, such as scene replacement/restoration; preserve stable asset paths across phases. Use `SerializedObject` for serialized changes where the examples do so.
3. **Postflight:** save, independently reload required generated assets from their canonical paths, and verify serialized references/values, scene bindings, expected cardinality, Camera Session, Routes/Activities, prefabs, and other contract-specific baseline requirements. Emit setup success only after this check.
4. **Cleanup:** enumerate QA-owned paths and environment mutations; delete/restore only owned state; handle partial preparation and re-execution; preserve authored/user content; report residuals; never publish a successful result before required cleanup/restoration.

Reloading is conditional after a demonstrated invalidation boundary and independent in postflight. No reload loop after every AssetDatabase operation.

### Review

Before the first Play Mode, independently trace `Sources → Preflight → Construction → Persisted Assets → Scenario → Evidence → Cleanup`. Inspect operation order for stale Unity wrappers, missing or wrongly typed references, invalid serialized links/cardinality, configuration side effects, partial-generation ownership, baseline gaps, and premature PASS. Classify QA, Framework, environment, and observability findings separately. Do not require Unity evidence for static defects that can already be identified; still label compile/import/runtime validation as pending when it was not run.

## Agent alignment

- Update `.codex/agents/qa-certification-architect.toml`, `qa-certification-implementer.toml`, and `qa-certification-reviewer.toml` to point at the shared reference and enforce their corresponding gates.
- Update `.claude/agents/qa-scenario-writer.md` and `qa-adr-auditor.md` to use the shared reference and the same evidence/validation boundaries.
- Update `.claude/skills/qa-scenario-design/SKILL.md` and `qa-greenfield-slice/SKILL.md` only where required to route into the shared authoring reference and preserve ADR-001 sequencing.
- Update the three `.agents/skills/qa-certification-*/SKILL.md` entry points and relevant existing references to keep compact role-specific workflows with no duplicated authoring manual.
- Leave agent/skill UI metadata untouched unless the static discovery audit demonstrates a concrete mismatch.

## Verification design

Static checks will cover skill frontmatter and names, agent TOML/YAML/Markdown structure, every relative reference from both runtimes, unchanged invocation policies, no dangling paths, no contradictory claims against ADR-001, and `git diff --check`. Documentation walkthroughs will apply Design to a hypothetical new QA using both preexisting and planned generated inputs, then apply Review to a pressure case where a typed GameApplication template is loaded successfully, a scene operation documented as potentially invalidating wrappers occurs, the old wrapper is reused, and postflight must independently reload and verify the persisted asset. The walkthrough must identify the ordering risk and require reacquisition at the appropriate boundary, without asserting that this was the proven cause of the historical IF-ADR-045 null.

No Unity validation is part of this task. The writing-skills method recommends independent agent pressure tests; current collaboration instructions prohibit spawning agents unless explicitly requested. These walkthroughs validate that the written instructions cover the cases by static review. They are not behavioral tests of an agent following the skills.

## Acceptance criteria

- The three existing Codex skills express distinct, compact gates and link to one shared authoring reference.
- Existing Claude skills and agents route to compatible evidence without duplicating the reference or gaining new roles.
- The shared reference is grounded in actual QA source symbols and accurately records conditions and side effects.
- Design verifies preexisting dependencies and accepts planned resources only with a supported materialization and post-persistence validation path; Implementation verifies persisted output; Review checks operation ordering before Play Mode.
- ADR-001 remains unchanged; no QA/runtime implementation or IF-ADR-045 files are changed.
- Static reference/configuration checks and `git diff --check` pass. Unity compilation and execution remain explicitly unclaimed.
