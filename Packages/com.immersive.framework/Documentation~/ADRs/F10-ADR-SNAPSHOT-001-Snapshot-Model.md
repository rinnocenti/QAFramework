# F10 ADR SNAPSHOT 001 - Snapshot Model

Status: Accepted

## Context

State capture must stay separate from reset semantics.

## Decision

Snapshot is different from Reset.

Snapshot has envelope, owner, schema, version and payload.

Reset Baseline is not Save Snapshot.

## Consequences

Future persistence can evolve independently from cycle reset and active-state reset.

## Guardrails

- Do not implement Reset as snapshot restore.
- Do not treat save payload as lifecycle owner.
- Version snapshot payloads explicitly.
