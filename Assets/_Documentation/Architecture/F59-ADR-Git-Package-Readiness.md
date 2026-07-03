# F59-ADR-Git-Package-Readiness

Status: Implemented locally / pending clean consumer validation
Date: 2026-07-03
Track: PACKAGE-1 / Git Package Readiness
Depends on: F34, F58

## Context

F58 added minimal Model/Authorship validation so a consumer project can check whether the active `GameApplicationAsset`, routes, scenes, `UIGlobal` and existing authoring declarations are ready.

The next package risk is installation. `Packages/com.immersive.framework` must be consumable through Unity Package Manager from Git without copying QA project assets, scenes, prefabs or `ProjectSettings` into the package.

Unity supports installing packages from Git URLs in the consumer project's `Packages/manifest.json`, including `#revision` for a branch, tag or hash. Unity package manifests do not support Git URLs as transitive dependencies inside a package `package.json`.

## Decision

F59 prepares `com.immersive.framework` for Git package consumption with version:

`1.0.0-preview.1`

F59 does not create a Git tag, publish a release, create a repository, move the package outside the current project or run clean consumer install validation. It documents the install process and fixes the framework package manifest so it no longer declares Git URLs as package dependencies.

## Package Version

- Package name: `com.immersive.framework`
- Package version: `1.0.0-preview.1`
- Recommended Git tag after validation: `v1.0.0-preview.1`
- Unity target: `6000.0`

The tag should be created only after Unity import/compile, Model Readiness and F60 clean consumer install validation pass.

## Package Manifest Rules

`Packages/com.immersive.framework/package.json` must:

- keep `name` as `com.immersive.framework`;
- use `version` `1.0.0-preview.1`;
- use a clear `displayName`;
- describe runtime, authoring, diagnostics and validation surfaces;
- declare only valid package/version dependencies;
- not contain Git URLs;
- not contain local absolute paths;
- not declare QA/project-only packages.

F59 sets framework dependencies to package/version entries only:

- `com.immersive.foundation`: `1.0.0-preview.1`
- `com.immersive.logging`: `1.0.0-preview.1`
- `com.unity.inputsystem`: `1.19.0`

## Dependency Policy

Real dependencies found by asmdef/source audit:

- `com.immersive.foundation`: required by runtime event primitives.
- `com.immersive.logging`: required by runtime/editor diagnostics and logging config usage.
- `com.unity.inputsystem`: required by runtime input/Pause/InputMode paths.

`com.immersive.pooling` is not a dependency of `com.immersive.framework` in this package version.

If `com.immersive.foundation` and `com.immersive.logging` are private Git packages and not published in a scoped registry, the consumer project must install them directly in its own `Packages/manifest.json`. They must not be represented as Git URLs in `com.immersive.framework/package.json`.

## Git Install Policy

Consumer manifest example after tags exist:

```json
{
  "dependencies": {
    "com.immersive.foundation": "https://github.com/ImmersiveGames/com.immersive.foundation.git#v1.0.0-preview.1",
    "com.immersive.logging": "https://github.com/ImmersiveGames/com.immersive.logging.git#v1.0.0-preview.1",
    "com.immersive.framework": "https://github.com/ImmersiveGames/com.immersive.framework.git#v1.0.0-preview.1"
  }
}
```

Install order:

1. `com.immersive.foundation`
2. `com.immersive.logging`
3. `com.immersive.framework`

If the sibling packages are available from a compatible scoped registry, the consumer may use registry resolution instead of direct Git entries for those siblings.

## Package Content Boundary

The package root should contain package-owned content only:

- `Runtime/`
- `Editor/`
- `Documentation~/`
- `README.md`
- `package.json`
- asmdefs and corresponding `.meta` files

F59 does not move or copy QA project assets into the package.

Historical ADRs and planning files under `Documentation~/` remain historical source material. The active user entry points are `README.md`, `Documentation~/README.md`, `Setup.md`, `Authoring.md`, `Troubleshooting.md`, `QA-Smokes.md` and `Git-Package-Install.md`.

## Consumer Project Requirements

A consumer project must:

- use Unity compatible with package target `6000.0`;
- have Git available to Unity when installing from Git URLs;
- install private sibling packages directly or configure a scoped registry;
- let Unity resolve `com.unity.inputsystem`;
- configure Model 1.0 authoring assets in the consumer project;
- run Project Settings > Immersive Framework > Model Readiness > `Run Model Readiness Check`;
- resolve blocking readiness issues before FIRSTGAME work.

## Rejected Scope

F59 does not:

- create a Git tag;
- publish a release;
- create a new repository;
- move the package physically out of the project;
- alter runtime or editor behavior;
- change asmdefs;
- alter scenes, prefabs, serialized assets, ProjectSettings, csproj or QA Canvas;
- edit the current project `Packages/manifest.json`;
- copy QA assets into the package;
- open FIRSTGAME.

## Validation Plan

Static validation:

1. `git diff --check`
2. Parse `Packages/com.immersive.framework/package.json` as JSON.
3. Confirm no Git URL exists in `com.immersive.framework/package.json`.
4. Confirm no local absolute path dependency exists in `com.immersive.framework/package.json`.
5. Confirm no scenes, prefabs, project settings, csproj, zips or QA assets were added to the package.
6. Confirm no runtime/editor behavior patch was made.
7. Confirm private Git dependencies are documented as consumer project manifest entries, not package transitive dependencies.

Unity validation required before closing:

1. Unity import/compile in the current project.
2. Run Model Readiness Check.
3. Confirm blocking issues are zero.

Clean consumer validation required before tagging:

1. Create or use a clean Unity consumer project.
2. Install private sibling packages by pinned tag or scoped registry.
3. Install `com.immersive.framework` by pinned tag.
4. Confirm import/compile.
5. Configure minimal Model 1.0 authoring.
6. Run Model Readiness with zero blocking issues.

## Next Gate

Recommended next gate:

`F60 - PACKAGE-2 - Clean Consumer Install Validation`

FIRSTGAME remains deferred until clean package consumption is validated.
