# P3D — Activity Participation Authoring QA

## Objective

Protect the current Activity Projection and Requirements authoring contract with one canonical Editor-only smoke after the QAFramework cleanup.

## Menu

```text
Immersive Framework
  QA
    Player
      P3D Run Activity Participation Authoring Smoke
```

Do not enter Play Mode. Clear the Console, run the menu command and inspect the final P3D log.

## Cases

```text
no-slots-none-valid
all-joined-zero-allowed-valid
explicit-slots-order-preserved
missing-projection-rejected
missing-requirements-rejected
no-slots-non-none-rejected
all-joined-explicit-list-rejected
explicit-empty-rejected
explicit-duplicate-rejected
validation-is-non-mutating
projection-template-set-created
project-scan-detects-invalid-activity
```

## Expected PASS

```text
[P3D_ACTIVITY_PARTICIPATION_AUTHORING_SMOKE] status='Passed' cases='12'
```

The template case intentionally creates three temporary Projection Profiles. All temporary assets are removed in `finally`.

## Model Readiness after the smoke

The smoke creates deliberate invalid fixtures, so it performs its own project-scan negative case before cleanup. After the smoke completes and removes the fixtures:

```text
Edit > Project Settings > Immersive Framework
  Run Model Readiness Check
```

Expected participation evidence:

```text
Activity participation project validation passed
P3D Player participation readiness aggregated
errors='0'
```

Every retained QA `ActivityAsset` must reference both:

```text
Activity Participation Projection Profile
Player Participation Requirements Profile
```

For an Activity that currently requires no Players:

```text
Projection: Activity Participation — No Players
Requirements: Player Participation — None
```

## Boundary

This smoke proves authoring data, templates, explicit failures, deterministic ordering and non-mutation. It does not prove Session state, runtime projection, Activity admission, join or Actor materialization.

## Suggested commit

```text
P3D.2 — add Activity participation authoring QA
```
