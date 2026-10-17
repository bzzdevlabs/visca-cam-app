---
sidebar_position: 3
title: CI and releases
---

# Continuous integration and releases

All workflows run on GitHub-hosted runners, which are free for public repositories.

## CI

`ci.yml` builds, unit-tests and smoke-tests every pull request:

- **Unit tests** of `TenveoPtz.Core` (presenter, VISCA and UVC control, presets, settings,
  updates, translations, end-to-end scenarios on the simulated camera). The CI fails when they
  cover less than **90 %** of its lines; the run page shows the coverage and the least covered
  files.
- **Windows**: the app is started in French (light theme) and in English (dark theme), the
  update from the latest release is tested, the real app is driven on the simulated camera
  (arrow, zoom, Shift and Home keys, presets, the settings panel, the numeric keypad while
  another app is in front), and the `.msi` is installed and uninstalled.
- **macOS**: the app is started in the light theme as Apple silicon code and in the dark theme
  as Intel code, through Rosetta.

The run page shows the version, a test report with every test's result, and screenshots of the
windows (stored on the `ci-screenshots` branch, which keeps the last 30 runs).

## Release

`release.yml` runs when a pull request is merged into `main`, and publishes a GitHub release
with the Windows `.exe` and `.msi` and the macOS `.dmg`.

## Documentation-only changes

Changes that only touch the documentation (`website/`, Markdown files, or the docs workflow)
do not start the CI or release workflows: they only rebuild this site (see
[Documentation](./docs.md)).

## Versioning

Versions are `major.minor.patch.build`, as Windows file versions:

- `major.minor.patch` comes from the [Conventional Commits](https://www.conventionalcommits.org/)
  since the last tag: `feat` bumps the minor version, `fix`/`perf` the patch, a breaking change
  the major.
- `build` is the number of commits in the history, so it grows with every change and a rebuild
  of the same commit gets the same number.

Merges with only `docs`, `ci`, `chore` or `test` commits are tested but not released. Run
`build/version.sh` to see the current version locally.
