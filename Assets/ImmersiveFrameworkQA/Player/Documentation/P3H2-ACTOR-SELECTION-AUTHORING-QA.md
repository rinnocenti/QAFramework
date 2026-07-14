# P3H.2 — Actor Selection Authoring QA

## Purpose

Prove the immutable `ActorProfile`, explicit duplicate-selection policy, optional Player Slot default and official policy templates before Session selection runtime is implemented.

## Run

Outside Play Mode:

```text
Immersive Framework
└── QA
    └── Player
        └── P3H.2 Run Actor Selection Authoring Smoke
```

## Expected

```text
[P3H2_ACTOR_SELECTION_AUTHORING_SMOKE] status='Passed' cases='18'
```

## Cases

```text
actor-profile-id-normalized
actor-profile-typed-identity
valid-player-actor-profile
empty-actor-profile-id-rejected
duplicate-actor-profile-id-rejected
missing-logical-host-rejected
unknown-actor-kind-rejected
mismatched-host-kind-rejected
allow-duplicates-policy-valid
unique-policy-valid
unspecified-policy-rejected
slot-default-is-optional
slot-default-reference-valid
invalid-slot-default-rejected
project-duplicate-scan-rejected
validation-is-non-mutating
profiles-are-runtime-immutable
policy-template-set-created
```

The smoke creates temporary assets and a temporary Player Actor Host prefab, validates them through the package Editor validators, executes the official policy template command and removes all temporary assets before completion.
