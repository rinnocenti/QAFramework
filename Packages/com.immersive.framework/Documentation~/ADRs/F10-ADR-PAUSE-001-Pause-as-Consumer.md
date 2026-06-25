# F10 ADR PAUSE 001 - Pause as Consumer

Status: Accepted

## Context

Pause crosses lifecycle, input and content visibility concerns, but it is not a core owner.

## Decision

Pause is a consumer of lifecycle, input and Content Anchor.

Pause does not own Route or Activity.

Pause does not define core Content Anchor.

## Consequences

Pause behavior can be layered on top of core contracts without redefining the framework.

## Guardrails

- Do not let Pause own Route or Activity state.
- Do not define anchor semantics inside Pause.
- Keep pause-specific behavior in consumer space.
