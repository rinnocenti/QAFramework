# Unity QA Authoring and Composition Reference

Use this reference when a QA needs authored or generated Unity assets, scenes, prefabs, or serialized composition. The examples are repository evidence, not a mandate to reproduce their whole setup. Inspect the current source and active package before relying on any pattern.

## Evidence map

| QA source | Verified pattern | Conditions and limits |
|---|---|---|
| `Assets/QA-NEW-004/Settings`, `Assets/QA-NEW-004/Scenes`, `Documentation~/QA-NEW-004-Certification.md` | A persisted, authored `GameApplication`, Route/Activity graph, scenes, camera assets, and a scenario can form an inspectable environment that is executed independently. | This proves the persisted-environment path, not a general asset generator or setup procedure. |
| `Assets/QA-IF-ADR-042/Editor/QaIfAdr042Setup.cs`: `Configure`, `CreateAsset`, `CreatePersistentScene`, `SetPersistentScene`, `ValidateGeneratedBaseline` | Typed loads are used for QA assets; setup saves and reloads assets between authoring phases; the generated baseline receives a final cross-asset validation. | This setup reuses and updates known QA-owned paths and modifies Editor Build Settings. Those effects are specific to its workflow. |
| `Assets/QA-IF-ADR-043/Editor/QaIfAdr043Setup.cs`: `Configure`, `ValidateGeneratedBaseline` | A generated baseline is persisted and checked before setup reports it configured. | Its setup changes build configuration. Inspect all environment mutations before reuse. |
| `Assets/QA-IF-ADR-044/Editor/QaIfAdr044Setup.cs`: `Configure`, `CreateApplication`, `CreatePersistentScene`, `BindPersistentScene`, `ValidateGeneratedBaseline` | A validated `GameApplication` template is copied to a QA-owned path, then loaded from the destination and edited. Scene-generation code explicitly notes that `NewScene(Single)` can invalidate wrappers loaded before the scene switch; it reloads assets after that boundary and reloads the application before binding the saved scene. A final method validates the generated baseline. | This is direct evidence of a Unity wrapper invalidation risk and a reacquisition pattern for that boundary. It does not establish that every AssetDatabase operation invalidates references. |
| `Assets/QA-NEW-005/Editor/QaNew005Setup.cs`: `Configure` and generated-scene methods | A multi-phase setup generates assets/scenes and restores captured Editor scene setup in `finally`. | Refresh/save placement belongs to that specific dependency graph; do not infer a universal need to refresh or reload after every AssetDatabase call. |
| `Assets/QA-IF-ADR-045/Editor/QaIfAdr045Setup.cs`: `Prepare`, `CreatePersistentScene`, `CreateApplication`, `LoadRequiredSourceAsset` | The setup has a typed source preflight, dirty/untitled-scene guard, QA-owned generated paths, scoped cleanup, and explicit serialization. It carries some Unity object references through scene operations that IF-ADR-044 documents as potentially invalidating wrappers. | Unity confirmed the source template was loadable, but the later null reference's exact cause remained inconclusive. Stale-wrapper reuse is a plausible risk case, not a proven root cause. Do not change D7 when using it as a documentation walkthrough. |

## Choose the environment first

Use a persisted environment when the required composition is stable, inspectable, and useful across runs. QA-NEW-004 demonstrates this path. Use generated assets when a contract requires isolation, specialized topology, controlled invalid authoring, or disposable state. Generated content must have an explicit owner and cleanup boundary, as the IF-ADR-045 generated folder illustrates.

Design the dependency graph before creating anything. Distinguish preexisting sources from planned outputs. Preexisting sources must resolve from the active workspace/package and load as the expected type. A planned output need not exist during preflight, but its type, purpose, inputs, supported authoring API, owner/lifetime, creation order, and persisted validation must be defined.

For each required asset, verify the aspects relevant to its role: exact asset path, GUID where identity or stable ownership matters, typed load, expected main-asset type where the file is copied, and required references/cardinality/function. A file existing on disk alone does not prove its script type or its ability to satisfy the composition. Validate semantic authoring too; examples include `CameraSessionConfiguration.TryValidate`, `SessionCameraAssignmentAsset.TryBuild`, Route scene availability, and exact scene contribution bindings.

## Order authoring around Unity reference lifetime

Use explicit asset paths as stable identifiers between phases. Keep Unity object references only while their lifetime is known to remain valid.

1. Capture and validate source paths and composition requirements before generation.
2. Before an operation documented or demonstrated to invalidate loaded Unity wrappers, retain stable paths/identities needed after it.
3. After the invalidating boundary, reload only the assets needed by the next authoring action. IF-ADR-044 demonstrates this after `NewScene(Single)` and scene restoration.
4. Bind freshly acquired assets into the active scene/prefab composition, check the authored links, and save the generated output.
5. Do not add AssetDatabase reloads mechanically after every call. The reference examples do not prove that `CopyAsset`, `CreateAsset`, `SaveAssets`, or `Refresh` always invalidates all wrappers. Check the specific API effect and repository precedent.

When distinguishing reference states in diagnostics, Unity objects have both CLR reference identity and Unity's overloaded null semantics. If a failure involves a possibly invalidated object, report both `ReferenceEquals(value, null)` and `value == null`, plus a stable path/GUID captured while the object was valid. Avoid dereferencing a Unity-null object merely to produce a diagnostic.

## Observe identity through lifecycle changes

When the contract observes transfer, unload, replacement, or recreation, capture the source Scene identity while it is valid. Retain its path and handle before unload when needed; correlate the unload event with that captured handle, and do not read `Scene.path` after unload as historical evidence. Retain original Unity object references and compare them separately from `EntityId`, Scene path/handle, and composition owner/lifetime. Assert only the stability or change required by the contract; these identity domains are not substitutes for one another.

IF-ADR-045 D7 illustrates the observability risk: early evidence reported an empty source path after unload and a root identity divergence, while the later scenario retained source identity before unload and reported the original roots transferred. The available record does not establish the historical cause of that first identity divergence.

## Copy and serialize a template

Use a template only when its required configuration is validated and the generated QA truly needs to preserve that configuration. QA-IF-ADR-044 demonstrates copying a GameApplication template; QA-IF-ADR-045 intended the same for Camera Session configuration.

- Validate the source as the expected typed asset before generation.
- Check the QA-owned destination policy before copying; never overwrite unknown or authored content silently.
- Copy the source asset to the exact generated path, then load the destination as the expected type. Do not continue editing the source object or assume the copy call's in-memory object is the persisted destination.
- Change only fields owned by the QA. Use `SerializedObject` when that is the established mechanism for private serialized authoring fields.
- Save the generated asset, reload it from its canonical path, and verify serialized values and references on that reloaded object.
- Validate the effective configuration used by the Scenario, including required Route/Activity references, Camera Session, and assignment cardinality when applicable.

Creation from scratch is appropriate only when no required source configuration must be preserved and all authored dependencies can be constructed and validated. Do not replace a required template with an incomplete blank asset.

## Generate and validate scenes

Scene operations can affect both the Editor's loaded scene set and the validity of Unity object wrappers. Follow the exact pattern supported by the chosen operation:

- Before `NewScene(Single)` or another operation shown to invalidate wrappers, verify dirty/untitled scene policy and capture the restorable scene setup. IF-ADR-045's `EnsureNoModifiedOpenScenes` and IF-ADR-044's captured setup/`RestoreSceneManagerSetup` demonstrate these protections.
- After the scene switch, reacquire source assets needed for component bindings by their canonical asset paths. Do not carry Route, Activity, GameApplication, prefab, or evidence references across the boundary solely because their CLR variables still exist.
- Bind the freshly loaded assets into components, verify exact component count and references, mark/save the scene, and restore the prior scene setup in `finally`.
- After restoring, reload the saved `SceneAsset` by path. For postflight, open or inspect the persisted scene through an appropriate read-only validation path and confirm its serialized bindings, not only the pre-save component instances.

`finally` guarantees that restoration is attempted; it does not prove that the scene setup was safely restorable or that authored changes were preserved. Preflight dirty-scene checks and postflight state checks provide that evidence.

## Persist, reacquire, and postflight

Treat generation, persistence, and validation as separate stages:

`Preflight → Materialization → Serialization → Persistence → Reacquisition → Validation`

Save assets/scenes at dependency boundaries needed by later phases. Reacquire from canonical paths when an operation may have replaced Unity wrappers and always perform an independent postflight load of final outputs. Validate a connected baseline rather than isolated file existence:

- expected typed asset exists at its owned path;
- its serialized references resolve to the expected QA assets;
- references point to the intended scene, Route, Activity, prefab, profile, and evidence objects;
- required cardinalities and semantic validators pass;
- generated scene/prefab contents contain the expected components and exact bindings;
- required environment settings are either explicitly configured by their owner or documented as manual preparation.

QA-IF-ADR-042 and QA-IF-ADR-044 use `ValidateGeneratedBaseline` as the final setup gate. Emit a `Configured`/`Prepared` setup result only after equivalent contract-specific postflight checks finish. This setup result is not a Unity compile, Play Mode, or certification PASS.

## Ownership, cleanup, and re-execution

The creator owns temporary assets and environment mutations. Before generation, list exact output paths and any project-level settings the setup may touch. Preserve assets outside that list.

- Refuse dirty or untitled scenes when setup/cleanup would replace or close them; do not silently save or discard user work.
- Reject collisions with unknown content. A partial prior run must have a scoped recovery path.
- Delete only exact QA-owned files and folders; remove folders only when empty. Report residual files rather than deleting unknown content.
- Restore temporary environment changes through the owner that captured them. If a shared Build Profile or Build Settings mutation is manual, document the prior value and exact restoration steps.
- Cleanup should be safe after partial preparation and on repeated invocation. Do not emit successful setup/evidence before required cleanup or restoration has completed.

IF-ADR-045 demonstrates a scoped generated-asset list and guards against dirty scenes and deleting an active generated application. QA-IF-ADR-042 and QA-IF-ADR-043 demonstrate build-list mutation, but that is not required for setups whose contract asks for manual scene inclusion.

## Evidence and validation states

Keep these claims distinct:

- **Implemented:** authoring/setup code and documentation exist.
- **Compiled/imported:** Unity reports scripts and assets imported without errors.
- **Executed:** the setup or Scenario ran and emitted its observed result.
- **Validated/certified:** the declared contract and cleanup conditions were proved by the required execution evidence.

Static checks can find ordering, type, path, serialized-reference, ownership, and evidence defects before Play Mode. They cannot prove AssetDatabase import, Unity compilation, runtime lifecycle, or Player Build behavior. Report those as pending until observed. A missing precondition or observability surface is `BLOCKED`; do not promote it to PASS. A failure after valid exercise is `FAIL`, preserving the first causal divergence and reporting cleanup separately.
