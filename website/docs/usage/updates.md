---
sidebar_position: 4
title: Updates
---

# Updates

On start-up the app checks the [Releases](https://github.com/bzzdevlabs/visca-cam-app/releases)
page for a newer version. When one exists it asks whether to install it:

- **Update** downloads it, replaces the app (`TenveoPTZ.exe` on Windows, `Tenveo PTZ.app` on
  macOS) and restarts it.
- **Not now** asks again on the next start.

![The update prompt](/img/screenshots/update-prompt.png)

Without internet access the check is silently skipped. If the app's folder is not writable, the
status line says so: download the new version by hand instead. On macOS the app must have been
moved out of the disk image (to **Applications**) to update itself.

Turn off **Look for a new version on start-up** in [Settings](./settings.md) to skip the check.

:::note

Versions released as a `.zip` (before the single `.exe`) cannot update themselves to newer
releases: download the `.exe` or `.msi` once by hand.

:::
