# IF-FW-F26A1 - Activity Content Execution Diagnostics Clarification

## Status
Accepted / Diagnostics + Documentation

## Context

F26A validated that Activity-owned additive scenes loaded by Activity scene composition are included in Activity content discovery through `ActivitySceneLedger`.

Validated smoke evidence included:

```text
activityContentDiscoverySceneRoots='1'
activityContentAnchorCandidates='1'
activityContentAnchors='1'
activityContentHandles='1'
activityContentBindings='1'
activityContentLifecycle='Executed'
activitySceneLedgerLoaded='1'
```

The remaining confusing point was `activityContentExecution='SucceededNoContent'` with `activityContentHandles='1'`.

## Decision

`activityContentExecution*` is participant execution diagnostics. It reports only participants supplied by `IActivityContentExecutionParticipantSource`.

Local Activity content is reported by:

```text
activityContentDiscoverySceneRoots
activityContentAnchors
activityContentAnchorCandidates
activityContentHandles
activityContentBindings
activityContentLifecycle
```

F26A1 keeps the existing `activityContentExecution*` fields for compatibility and adds explicit participant aliases:

```text
activityContentParticipantExecution
activityContentParticipantSource
activityContentParticipantSourceIssues
activityContentParticipantCount
activityContentParticipantEnter
activityContentParticipantEnterRequests
activityContentParticipantExit
activityContentParticipantExitRequests
activityContentParticipantBlockingIssues
activityContentParticipantBlocksReadiness
```

## Non-goals

- No participant auto-discovery.
- No Activity scene discovery behavior change.
- No visual/loading semantic change.
- No Loading Progress work.
- No Pause, Save, Player, Camera, Audio, Addressables or gameplay adapter changes.

## Reading Logs

If `activityContentHandles='1'` and `activityContentLifecycle='Executed'`, local Activity adapters were discovered and processed.

If `activityContentParticipantExecution='SucceededNoContent'`, no explicit participant source supplied participant execution requests. That does not mean local Activity content was missing.
