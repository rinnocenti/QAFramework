# F60 ADR - Sync Local Framework Package Repository

Status: Implemented locally / pending clean consumer validation

Date: 2026-07-03

## Context

F58 owner-validated minimal Model Readiness with zero reported errors, warnings and blocking issues. F59 prepared `Packages/com.immersive.framework` for UPM/Git consumption as package version `1.0.0-preview.1`.

The framework package now needs to exist as the root content of the dedicated package repository:

`https://github.com/ImmersiveGames/com.immersive.framework`

## Decision

Synchronize the current `Packages/com.immersive.framework` package content into the local dedicated repository root for `com.immersive.framework`.

The package repository root must contain `package.json`, `README.md`, `Runtime`, `Editor`, `Documentation~`, asmdefs, `.meta` files and other files already inside the package. It must not contain a nested `Packages/com.immersive.framework` directory.

## Package Boundary

Only package content is synchronized.

Do not copy project consumer content:

- `Assets`
- project architecture ADR/tracker files
- QA scenes or prefabs
- `ProjectSettings`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- generated IDE/project files
- local backups or zips

## Repository Policy

The expected remote is:

`https://github.com/ImmersiveGames/com.immersive.framework`

F60 does not create a tag, push, publish a release or commit automatically. The recommended local commit message after review is:

`chore: prepare framework package 1.0.0-preview.1`

## Validation Plan

Validate in the package repository:

- `package.json` parses as JSON.
- `name` is `com.immersive.framework`.
- `version` is `1.0.0-preview.1`.
- package dependencies use version strings, not Git URLs or local paths.
- package root contains `Runtime`, `Editor`, `Documentation~`, `README.md` and `package.json`.
- forbidden project artifacts are absent.
- docs, `package.json` and asmdefs do not contain local absolute paths.
- `git diff --check` passes.
- `git status` clearly shows the synchronized package changes.

## Rejected Scope

F60 does not alter runtime/editor behavior, asmdefs, the Unity project manifest, project settings, QA assets, package sibling dependencies, FIRSTGAME, tags, pushes or releases.

## Next Gate

`F61 - PACKAGE-3 - Clean Consumer Install Validation`
