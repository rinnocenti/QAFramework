# Immersive Framework Documentation

This folder contains product and architecture documentation for the Immersive Framework workspace.

## Active architecture navigation

- Architecture decision file: `Assets/_Documentation/Architecture/F34-ADR-Architecture-Consolidation.md`
- Immutable consolidation plan: `Assets/_Documentation/Architecture/F34-PLAN-Architecture-Consolidation.v1.md`
- Mutable consolidation tracker: `Assets/_Documentation/Architecture/F34-TRACK-Architecture-Consolidation.md`

## Documentation model

Architecture documentation is separated by responsibility:

- `F34-ADR-Architecture-Consolidation.md` records active architectural decisions.
- `F34-PLAN-Architecture-Consolidation.v1.md` records the accepted route and should remain stable after acceptance.
- `F34-TRACK-Architecture-Consolidation.md` records real status, executed cuts, drift, pending gates, and validation notes.

Progress is not edited into immutable plans. Update the tracker when a track advances, drifts, or needs a new gate.

## Historical source material

The legacy folders `ADRs/`, `Audits/`, `Plans/`, `Closeouts/`, and `Notes/` contain historical source material from earlier cuts. They are not required for active architecture navigation unless a current task explicitly names one of those files as a source.

Do not delete user/game-designer guides or product-facing documentation during architecture documentation cleanup unless a later cut explicitly scopes that work.
